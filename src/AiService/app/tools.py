import json
from pathlib import Path

import httpx
from pydantic import ValidationError

from .contracts import MAX_RESPONSE_BYTES, ServiceError, ToolResult, parse_json, tool_error, utf16_length
from .settings import Settings

TOOL_DEFINITIONS = json.loads(Path(__file__).with_name("tool_definitions.json").read_text(encoding="utf-8"))
KNOWN_TOOLS = frozenset(item["function"]["name"] for item in TOOL_DEFINITIONS)


async def bounded_response(response: httpx.Response, limit: int = MAX_RESPONSE_BYTES) -> bytes:
    size = 0
    chunks = []
    async for chunk in response.aiter_bytes():
        size += len(chunk)
        if size > limit:
            raise ServiceError("InvalidResponse")
        chunks.append(chunk)
    return b"".join(chunks)


class DotnetTools:
    def __init__(self, settings: Settings, client: httpx.AsyncClient):
        # Construct once from startup configuration, never from model/request data.
        self.url = settings.dotnet_base_url + "/internal/scholarbot/tools"
        self.token = settings.service_token.get_secret_value()
        self.client = client

    async def execute(self, name: str, arguments: str, context_token: str,
                      remaining_tokens: int, max_output_tokens: int) -> ToolResult:
        if name not in KNOWN_TOOLS:
            return tool_error("UnknownTool", "Alat yang diminta tidak tersedia.")
        # Preserve the original domain error for arguments accepted by the native
        # protocol but larger than the .NET tool's own bound. Do not send a 400.
        if not arguments or utf16_length(arguments) > 8000:
            return tool_error("INVALID_ARGUMENTS", "Argumen tool tidak valid atau terlalu panjang.")
        body = {"name": name, "arguments": arguments, "remainingTokens": remaining_tokens,
                "maxOutputTokens": min(max_output_tokens, remaining_tokens)}
        # UTF-8 without ASCII escaping keeps the .NET 32768-byte envelope bound
        # useful for Indonesian/non-BMP arguments too.
        encoded = json.dumps(body, ensure_ascii=False, separators=(",", ":")).encode("utf-8")
        if len(encoded) > 32768:
            return tool_error("INVALID_ARGUMENTS", "Argumen tool tidak valid atau terlalu panjang.")
        try:
            async with self.client.stream("POST", self.url, content=encoded, headers={
                "Authorization": "Bearer " + self.token,
                "X-ScholarBot-Context": context_token,
                "Content-Type": "application/json",
            }) as response:
                if response.status_code in {408, 504}:
                    raise ServiceError("Timeout")
                if response.status_code == 401:
                    raise ServiceError("Unauthorized")
                if response.status_code != 200:
                    raise ServiceError("Unavailable")
                payload = await bounded_response(response)
            return ToolResult.model_validate(parse_json(payload))
        except httpx.TimeoutException:
            raise ServiceError("Timeout") from None
        except httpx.HTTPError:
            raise ServiceError("Unavailable") from None
        except (ValidationError, ValueError, UnicodeError, RecursionError):
            raise ServiceError("InvalidResponse") from None
