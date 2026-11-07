import math

import httpx
from openai import APIConnectionError, APIStatusError, APITimeoutError, AsyncOpenAI

from .contracts import MAX_RESPONSE_BYTES, ServiceError, Usage, parse_json
from .settings import Settings


def nonnegative_integer(value) -> int:
    if type(value) is not int or not 0 <= value <= 2147483647:
        raise ServiceError("InvalidResponse")
    return value


def provider_usage(payload: dict) -> Usage:
    raw = payload.get("usage")
    if not isinstance(raw, dict):
        raise ServiceError("InvalidResponse")
    input_tokens = nonnegative_integer(raw.get("prompt_tokens"))
    output_tokens = nonnegative_integer(raw.get("completion_tokens"))
    if "total_tokens" in raw and nonnegative_integer(raw["total_tokens"]) != input_tokens + output_tokens:
        raise ServiceError("InvalidResponse")
    cost = raw.get("cost")
    if cost is not None and (type(cost) not in {int, float} or not math.isfinite(cost) or cost < 0):
        raise ServiceError("InvalidResponse")
    return Usage(inputTokens=input_tokens, outputTokens=output_tokens, apiCalls=1, costUsd=cost)


async def bounded_response(response: httpx.Response, limit: int = MAX_RESPONSE_BYTES) -> bytes:
    size = 0
    chunks = []
    async for chunk in response.aiter_bytes():
        size += len(chunk)
        if size > limit:
            raise ServiceError("InvalidResponse")
        chunks.append(chunk)
    return b"".join(chunks)


class OpenRouterProvider:
    def __init__(self, settings: Settings, async_openai_client: AsyncOpenAI, tool_definitions: list):
        self.settings = settings
        self.client = async_openai_client
        self.tool_definitions = tool_definitions

    async def complete(self, messages: list[dict], max_output_tokens: int) -> dict:
        return await self._complete(
            model=self.settings.model, messages=messages, max_tokens=max_output_tokens,
            tools=self.tool_definitions, parallel_tool_calls=False,
        )

    async def complete_matching(self, messages: list[dict], max_output_tokens: int) -> dict:
        return await self._complete(
            model=self.settings.matching_model, messages=messages, max_tokens=max_output_tokens,
            response_format={"type": "json_object"},
        )

    async def _complete(self, **parameters) -> dict:
        try:
            async with self.client.chat.completions.with_streaming_response.create(**parameters) as response:
                # Read raw streaming bytes before parsing; SDK retries are disabled at startup.
                payload = parse_json(await bounded_response(response.http_response))
            if not isinstance(payload, dict):
                raise ServiceError("InvalidResponse")
            return payload
        except (APITimeoutError, httpx.TimeoutException):
            raise ServiceError("Timeout") from None
        except APIStatusError as error:
            raise ServiceError("Timeout" if error.status_code in {408, 504} else "Unavailable") from None
        except (APIConnectionError, httpx.HTTPError):
            raise ServiceError("Unavailable") from None
        except (ValueError, UnicodeError, RecursionError):
            raise ServiceError("InvalidResponse") from None
