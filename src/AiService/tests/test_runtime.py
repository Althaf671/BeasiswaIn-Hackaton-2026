import asyncio

import pytest

from app.contracts import ChatRequest, ServiceError, ToolResult, Usage
from app.main import until_disconnect
from app.runtime import ScholarRuntime
from app.settings import Settings


def settings(**overrides):
    return Settings(SCHOLARBOT_SERVICE_TOKEN="s" * 32, OPENROUTER_API_KEY="test-only-key", **overrides)


def native(call_id="call_1", name="GetUserProfile", arguments="{}"):
    return {"id": call_id, "type": "function", "function": {"name": name, "arguments": arguments}}


def completion(calls=None, finish="stop", content="Jawaban", usage=None):
    return {"choices": [{"message": {"role": "assistant", "content": content, "tool_calls": calls or []}, "finish_reason": finish}],
            "usage": usage or {"prompt_tokens": 10, "completion_tokens": 5, "cost": 0.1}}


class ScriptedProvider:
    def __init__(self, outputs):
        self.outputs = iter(outputs)
        self.messages = None

    async def complete(self, messages, max_output_tokens):
        self.messages = messages
        return next(self.outputs)


class RecordedTools:
    def __init__(self, result=None):
        self.calls = []
        self.result = result or ToolResult(content='{"profile":true}', success=True, sources=[],
                                         usage=Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0))

    async def execute(self, *args):
        self.calls.append(args)
        return self.result


def chat_request():
    return ChatRequest(message="beasiswa", history=[], contextToken="opaque-not-in-prompt")


async def test_native_calls_on_stop_consume_nested_budget_without_exposing_delegation():
    provider = ScriptedProvider([completion([native()]), completion()])
    tools = RecordedTools(ToolResult(content='{"matched":true}', success=True, sources=[],
                                    usage=Usage(inputTokens=100, outputTokens=20, apiCalls=2, costUsd=None)))
    result = await ScholarRuntime(settings(), provider, tools).chat(chat_request())
    assert result["usage"] == {"inputTokens": 120, "outputTokens": 30, "apiCalls": 4, "costUsd": None}
    assert "opaque-not-in-prompt" not in repr(provider.messages)


@pytest.mark.parametrize("outputs,options,expected", [
    ([completion([native(), native()])], {}, "InvalidResponse"),
    ([completion([native(str(i)) for i in range(9)])], {}, "BudgetExceeded"),
    ([completion([native()]), completion([native("last")])], {"SCHOLARBOT_MAX_ITERATIONS": 2}, "BudgetExceeded"),
    ([completion(finish="length")], {}, "InvalidResponse"),
    ([completion(finish="content_filter")], {}, "InvalidResponse"),
    ([completion(content=" ")], {}, "InvalidResponse"),
    ([completion(finish="tool_calls")], {}, "InvalidResponse"),
    ([completion(usage={"prompt_tokens": 47995, "completion_tokens": 5})], {}, "BudgetExceeded"),
    ([completion(usage={"prompt_tokens": 1, "completion_tokens": 6001})], {}, "InvalidResponse"),
])
async def test_invalid_or_overbudget_batches_never_execute(outputs, options, expected):
    tools = RecordedTools()
    with pytest.raises(ServiceError, match=expected):
        await ScholarRuntime(settings(**options), ScriptedProvider(outputs), tools).chat(chat_request())
    assert len(tools.calls) == (1 if len(outputs) == 2 else 0)


async def test_nested_usage_enforced_before_next_call_even_on_unusable_result():
    tools = RecordedTools(ToolResult(content="[]", success=True, sources=[],
                                    usage=Usage(inputTokens=47980, outputTokens=5, apiCalls=1, costUsd=0)))
    provider = ScriptedProvider([completion([native(), native("second")])])
    with pytest.raises(ServiceError, match="BudgetExceeded"):
        await ScholarRuntime(settings(), provider, tools).chat(chat_request())
    assert len(tools.calls) == 1


@pytest.mark.parametrize("phase", ["provider", "callback"])
async def test_disconnect_cancels_paid_provider_or_nested_callback(phase):
    started = asyncio.Event()
    cancelled = asyncio.Event()
    disconnect = asyncio.Event()

    async def block():
        started.set()
        try:
            await asyncio.Future()
        finally:
            cancelled.set()

    class Provider:
        async def complete(self, messages, max_output_tokens):
            if phase == "provider":
                return await block()
            return completion([native(name="GetRecommendations", arguments='{"essay":"stated context"}')])

    class Tools:
        async def execute(self, *args):
            return await block()

    async def receive():
        await disconnect.wait()
        return {"type": "http.disconnect"}

    work = ScholarRuntime(settings(), Provider(), Tools()).chat(chat_request())
    task = asyncio.create_task(until_disconnect(work, receive))
    await asyncio.wait_for(started.wait(), 1)
    disconnect.set()
    with pytest.raises(asyncio.CancelledError):
        await asyncio.wait_for(task, 1)
    assert cancelled.is_set()


async def test_outer_timeout_also_cancels_paid_work():
    cancelled = asyncio.Event()

    async def paid_work():
        try:
            await asyncio.Future()
        finally:
            cancelled.set()

    async def connected():
        await asyncio.Future()

    with pytest.raises(TimeoutError):
        async with asyncio.timeout(0.01):
            await until_disconnect(paid_work(), connected)
    assert cancelled.is_set()
