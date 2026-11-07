import json
import math
from datetime import datetime
from typing import Literal
from urllib.parse import urlsplit

from pydantic import BaseModel, ConfigDict, Field, field_validator, model_validator

MAX_BODY_BYTES = 262144
MAX_RESPONSE_BYTES = 1048576

ERRORS = {
    "Unauthorized": (401, "Silakan masuk untuk menggunakan ScholarBot."),
    "InvalidRequest": (400, "Pesan atau riwayat percakapan tidak valid. Batasi pesan/esai hingga 12.000 karakter dan mulai percakapan baru jika riwayat terlalu panjang."),
    "Unavailable": (503, "ScholarBot belum dapat menghubungi layanan AI. Silakan coba lagi nanti."),
    "Timeout": (408, "Permintaan ScholarBot melewati batas waktu. Coba pertanyaan yang lebih singkat."),
    "BudgetExceeded": (400, "Batas pemrosesan tercapai. Sempitkan pertanyaan atau mulai percakapan baru."),
    "InvalidResponse": (503, "Jawaban AI belum lengkap atau tidak dapat digunakan. Silakan coba lagi."),
}


class ServiceError(Exception):
    def __init__(self, code: str):
        self.code = code
        self.status, self.safe_message = ERRORS[code]
        super().__init__(code)

    def envelope(self) -> dict:
        return {"error": {"code": self.code, "message": self.safe_message}}


def utf16_length(value: str) -> int:
    # Strict encoding also rejects JSON-escaped unpaired surrogates.
    return len(value.encode("utf-16-le", errors="strict")) // 2


def text_limit(value: str, limit: int, *, nonempty: bool = True) -> str:
    try:
        size = utf16_length(value)
    except UnicodeError:
        raise ValueError("Invalid Unicode") from None
    if size > limit or (nonempty and not value.strip()):
        raise ValueError("Invalid text length")
    return value


def parse_json(value: str | bytes):
    def pairs(items):
        result = {}
        for key, item in items:
            if key in result:
                raise ValueError("Duplicate JSON property")
            result[key] = item
        return result

    def invalid_constant(_):
        raise ValueError("Invalid JSON number")

    return json.loads(value, object_pairs_hook=pairs, parse_constant=invalid_constant)


class Contract(BaseModel):
    model_config = ConfigDict(extra="forbid", strict=True, hide_input_in_errors=True)


class HistoryMessage(Contract):
    role: Literal["user", "assistant"]
    content: str

    @model_validator(mode="after")
    def bounded(self) -> "HistoryMessage":
        text_limit(self.content, 12000 if self.role == "user" else 24000)
        return self


class ChatRequest(Contract):
    message: str = Field(description="Scholarship question or essay; at most 12,000 UTF-16 code units.")
    history: list[HistoryMessage] = Field(max_length=12, description="User/assistant turns only; total conversation at most 48,000 UTF-16 code units.")
    userId: str = Field(description="Stored account ID supplied exclusively by the trusted authenticated backend, never selected by the browser or model. At most 256 UTF-16 code units.")

    @field_validator("message")
    @classmethod
    def bounded_message(cls, value: str) -> str:
        return text_limit(value, 12000)

    @field_validator("userId")
    @classmethod
    def bounded_user_id(cls, value: str) -> str:
        return text_limit(value, 256)

    @model_validator(mode="after")
    def bounded_context_text(self) -> "ChatRequest":
        if utf16_length(self.message) + sum(utf16_length(item.content) for item in self.history) > 48000:
            raise ValueError("Context exceeds character cap")
        return self


class Usage(Contract):
    inputTokens: int = Field(ge=0, le=2147483647)
    outputTokens: int = Field(ge=0, le=2147483647)
    apiCalls: int = Field(ge=0, le=2147483647)
    costUsd: float | None

    @field_validator("costUsd")
    @classmethod
    def finite_cost(cls, value: float | None) -> float | None:
        if value is not None and (not math.isfinite(value) or value < 0):
            raise ValueError("Invalid cost")
        return value

    @model_validator(mode="after")
    def consistent_calls(self) -> "Usage":
        if self.apiCalls == 0 and (self.inputTokens != 0 or self.outputTokens != 0 or self.costUsd != 0):
            raise ValueError("Usage without a provider call")
        return self


class Source(Contract):
    id: str
    name: str
    registrationUrl: str | None
    guideUrl: str | None
    deadline: str

    @field_validator("id")
    @classmethod
    def bounded_id(cls, value: str) -> str:
        return text_limit(value, 256)

    @field_validator("name")
    @classmethod
    def bounded_name(cls, value: str) -> str:
        return text_limit(value, 1000)

    @field_validator("registrationUrl", "guideUrl")
    @classmethod
    def safe_url(cls, value: str | None) -> str | None:
        if value is None:
            return None
        text_limit(value, 4096)
        parsed = urlsplit(value)
        if parsed.scheme not in {"http", "https"} or not parsed.hostname or parsed.username or parsed.password or any(ord(c) <= 32 for c in value):
            raise ValueError("Invalid source URL")
        return value

    @field_validator("deadline")
    @classmethod
    def valid_deadline(cls, value: str) -> str:
        text_limit(value, 64)
        date = datetime.fromisoformat(value.replace("Z", "+00:00"))
        if date.tzinfo is None:
            raise ValueError("Missing deadline offset")
        return value


class ToolCallTrace(Contract):
    name: str
    success: bool
    durationMilliseconds: int = Field(ge=0)


class ChatResponse(Contract):
    answer: str
    sources: list[Source]
    toolCalls: list[ToolCallTrace]
    usage: Usage


class ErrorDetail(Contract):
    code: Literal["Unauthorized", "InvalidRequest", "Unavailable", "Timeout", "BudgetExceeded", "InvalidResponse"]
    message: str


class ErrorResponse(Contract):
    error: ErrorDetail


class HealthResponse(Contract):
    status: Literal["ok"]


class ToolResult(Contract):
    content: str
    success: bool
    sources: list[Source] = Field(max_length=100)
    usage: Usage


def tool_error(code: str, message: str) -> ToolResult:
    return ToolResult(content=json.dumps({"error": {"code": code, "message": message}}, ensure_ascii=False),
                      success=False, sources=[], usage=Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0))


def normalize_tool_result(result: ToolResult, limit: int) -> ToolResult:
    try:
        if not result.content.strip():
            return tool_error("InvalidToolResult", "Alat tidak mengembalikan data yang dapat digunakan.")
        if utf16_length(result.content) > limit:
            return tool_error("ToolResultTooLarge", "Hasil alat terlalu panjang. Sempitkan pertanyaan atau kurangi jumlah hasil.")
        if not isinstance(parse_json(result.content), dict):
            raise ValueError("Not an object")
    except (ValueError, UnicodeError, RecursionError):
        return tool_error("InvalidToolResult", "Format hasil alat tidak dapat digunakan.")
    return result
