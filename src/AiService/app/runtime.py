import logging
import math
import time
from dataclasses import dataclass

from .contracts import ChatRequest, ServiceError, Usage, normalize_tool_result, text_limit
from .instructions import create_instructions
from .settings import Settings
from .provider import OpenRouterProvider, provider_usage
from .tools import KNOWN_TOOLS, DataTools

logger = logging.getLogger(__name__)


class UsageLedger:
    def __init__(self, limit: int):
        self.limit = limit
        self.input_tokens = 0
        self.output_tokens = 0
        self.api_calls = 0
        self.cost = 0.0
        self.all_costs_known = True

    @property
    def remaining(self) -> int:
        return max(0, self.limit - self.input_tokens - self.output_tokens)

    def record(self, usage: Usage) -> None:
        self.input_tokens += usage.inputTokens
        self.output_tokens += usage.outputTokens
        self.api_calls += usage.apiCalls
        if usage.costUsd is None:
            self.all_costs_known = False
        else:
            self.cost += usage.costUsd
            if not math.isfinite(self.cost):
                raise ServiceError("InvalidResponse")
        if self.remaining <= 0:
            raise ServiceError("BudgetExceeded")

    def snapshot(self) -> dict:
        return {"inputTokens": self.input_tokens, "outputTokens": self.output_tokens,
                "apiCalls": self.api_calls, "costUsd": self.cost if self.all_costs_known else None}


@dataclass(frozen=True)
class Completion:
    message: dict
    answer: str
    calls: list[dict]
    finish_reason: str


def parse_completion(payload: dict) -> Completion:
    choices = payload.get("choices")
    if not isinstance(choices, list) or len(choices) != 1 or not isinstance(choices[0], dict):
        raise ServiceError("InvalidResponse")
    choice = choices[0]
    message = choice.get("message")
    finish = choice.get("finish_reason")
    if (not isinstance(message, dict) or message.get("role") != "assistant"
            or not isinstance(finish, str) or finish not in {"stop", "tool_calls"}):
        raise ServiceError("InvalidResponse")
    refusal = message.get("refusal")
    if refusal is not None and (not isinstance(refusal, str) or refusal.strip()):
        raise ServiceError("InvalidResponse")
    content = message.get("content")
    answer = "" if content is None else content
    if not isinstance(answer, str):
        raise ServiceError("InvalidResponse")
    try:
        text_limit(answer, 24000, nonempty=False)
    except ValueError:
        raise ServiceError("InvalidResponse") from None
    calls = message.get("tool_calls")
    if calls is None:
        calls = []
    if not isinstance(calls, list):
        raise ServiceError("InvalidResponse")
    # Only native protocol fields are echoed. Never echo arbitrary provider metadata.
    return Completion({"role": "assistant", "content": content}, answer, calls, finish)


def validate_calls(calls: list[dict]) -> list[dict]:
    ids = set()
    validated = []
    try:
        for call in calls:
            if not isinstance(call, dict) or call.get("type") != "function":
                raise ValueError("Invalid call")
            call_id = call.get("id")
            function = call.get("function")
            if not isinstance(call_id, str) or not isinstance(function, dict):
                raise ValueError("Invalid native ID")
            text_limit(call_id, 256)
            if call_id in ids:
                raise ValueError("Repeated native ID")
            ids.add(call_id)
            name, arguments = function.get("name"), function.get("arguments")
            if not isinstance(name, str) or not isinstance(arguments, str):
                raise ValueError("Invalid function")
            text_limit(name, 64)
            text_limit(arguments, 16000, nonempty=False)
            if len(arguments.encode("utf-8")) > 64000:
                raise ValueError("Arguments too large")
            validated.append({"id": call_id, "type": "function", "function": {"name": name, "arguments": arguments}})
    except (ValueError, UnicodeError):
        raise ServiceError("InvalidResponse") from None
    return validated


class ScholarRuntime:
    def __init__(self, settings: Settings, provider: OpenRouterProvider, tools: DataTools):
        self.settings = settings
        self.provider = provider
        self.tools = tools

    async def chat(self, request: ChatRequest) -> dict:
        if not await self.tools.user_exists(request.userId):
            raise ServiceError("Unauthorized")
        limits = self.settings
        ledger = UsageLedger(limits.max_total_tokens)
        messages = [{"role": "system", "content": create_instructions()}]
        messages.extend({"role": item.role, "content": item.content} for item in request.history)
        messages.append({"role": "user", "content": request.message})
        sources, traces = {}, []
        call_count = 0
        for iteration in range(limits.max_iterations):
            if ledger.remaining <= 0:
                raise ServiceError("BudgetExceeded")
            output_limit = min(limits.max_output_tokens, ledger.remaining)
            payload = await self.provider.complete(messages, output_limit)
            usage = provider_usage(payload)
            logger.info("ScholarBot provider inputTokens=%s outputTokens=%s costUsd=%s",
                        usage.inputTokens, usage.outputTokens, usage.costUsd)
            ledger.record(usage)
            if usage.outputTokens > output_limit:
                raise ServiceError("InvalidResponse")
            completion = parse_completion(payload)
            if not completion.calls:
                if completion.finish_reason != "stop" or not completion.answer.strip():
                    raise ServiceError("InvalidResponse")
                return {"answer": completion.answer.strip(), "sources": list(sources.values()),
                        "toolCalls": traces, "usage": ledger.snapshot()}
            # Check the complete native batch before executing even its first call.
            if len(completion.calls) > limits.max_tool_calls - call_count or iteration + 1 >= limits.max_iterations:
                raise ServiceError("BudgetExceeded")
            call_count += len(completion.calls)
            calls = validate_calls(completion.calls)
            messages.append({**completion.message, "tool_calls": calls})
            for call in calls:
                function = call["function"]
                known = function["name"] in KNOWN_TOOLS
                trace_name = function["name"] if known else "UnknownTool"
                started = time.monotonic()
                result = await self.tools.execute(function["name"], function["arguments"], request.userId,
                                                  ledger.remaining, min(limits.max_output_tokens, ledger.remaining))
                # Nested matching is charged even if its result must be normalized.
                nested_usage = result.usage
                ledger.record(result.usage)
                result = normalize_tool_result(result, limits.max_tool_result_characters)
                duration = int((time.monotonic() - started) * 1000)
                traces.append({"name": trace_name, "success": result.success, "durationMilliseconds": duration})
                logger.info("ScholarBot tool name=%s success=%s durationMilliseconds=%s inputTokens=%s outputTokens=%s apiCalls=%s costUsd=%s",
                            trace_name, result.success, duration, nested_usage.inputTokens,
                            nested_usage.outputTokens, nested_usage.apiCalls, nested_usage.costUsd)
                if result.success:
                    for source in result.sources:
                        sources.setdefault(source.id, source.model_dump())
                messages.append({"role": "tool", "tool_call_id": call["id"], "content": result.content})
        raise ServiceError("BudgetExceeded")
