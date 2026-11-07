import re
from urllib.parse import urlsplit

from pydantic import Field, SecretStr, field_validator, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=None, extra="ignore", hide_input_in_errors=True)

    service_token: SecretStr = Field(validation_alias="SCHOLARBOT_SERVICE_TOKEN")
    openrouter_api_key: SecretStr = Field(validation_alias="OPENROUTER_API_KEY")
    openrouter_base_url: str = Field(default="https://openrouter.ai/api/v1", validation_alias="OPENROUTER_BASE_URL")
    model: str = Field(default="openai/gpt-6-luna", validation_alias="SCHOLARBOT_MODEL")
    dotnet_base_url: str = Field(default="http://127.0.0.1:5009", validation_alias="DOTNET_BASE_URL")
    max_iterations: int = Field(default=6, ge=2, le=10, validation_alias="SCHOLARBOT_MAX_ITERATIONS")
    max_tool_calls: int = Field(default=8, ge=1, le=12, validation_alias="SCHOLARBOT_MAX_TOOL_CALLS")
    timeout_seconds: int = Field(default=90, ge=10, le=180, validation_alias="SCHOLARBOT_TIMEOUT_SECONDS")
    max_output_tokens: int = Field(default=6000, ge=256, le=10000, validation_alias="SCHOLARBOT_MAX_OUTPUT_TOKENS")
    max_total_tokens: int = Field(default=48000, ge=4000, le=64000, validation_alias="SCHOLARBOT_MAX_TOTAL_TOKENS")
    max_tool_result_characters: int = Field(default=16000, ge=2000, le=24000, validation_alias="SCHOLARBOT_MAX_TOOL_RESULT_CHARACTERS")

    @field_validator("service_token")
    @classmethod
    def valid_token(cls, value: SecretStr) -> SecretStr:
        if re.fullmatch(r"[A-Za-z0-9_-]{32,}", value.get_secret_value()) is None:
            raise ValueError("Service token must contain at least 32 URL-safe ASCII characters")
        return value

    @field_validator("openrouter_api_key")
    @classmethod
    def valid_key(cls, value: SecretStr) -> SecretStr:
        key = value.get_secret_value()
        if not key or key != key.strip() or not key.isascii() or any(ord(c) < 33 or ord(c) > 126 for c in key):
            raise ValueError("A valid API key is required")
        return value

    @field_validator("openrouter_base_url", "dotnet_base_url")
    @classmethod
    def valid_url(cls, value: str) -> str:
        try:
            parsed = urlsplit(value)
            port = parsed.port
        except ValueError:
            raise ValueError("Invalid service URL") from None
        if (parsed.scheme not in {"http", "https"} or not parsed.hostname or parsed.username is not None
                or parsed.password is not None or parsed.query or parsed.fragment or value != value.strip()
                or any(ord(c) <= 32 or ord(c) >= 127 for c in value) or "\\" in value
                or (port is not None and not 1 <= port <= 65535)):
            raise ValueError("Service URL must be absolute HTTP(S), without credentials, query or fragment")
        return value.rstrip("/")

    @field_validator("model")
    @classmethod
    def valid_model(cls, value: str) -> str:
        if not value.strip() or len(value) > 256 or any(ord(c) < 32 for c in value):
            raise ValueError("Invalid model")
        return value

    @model_validator(mode="after")
    def valid_budget(self) -> "Settings":
        if self.max_output_tokens > self.max_total_tokens:
            raise ValueError("Output token cap cannot exceed total token cap")
        return self
