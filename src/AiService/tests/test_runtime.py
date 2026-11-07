import asyncio

import pytest

from app.contracts import ChatRequest, ServiceError, Source, ToolResult, Usage
from app.main import until_disconnect
from app.runtime import ScholarRuntime
from app.settings import Settings


def settings(**overrides):
    return Settings(SCHOLARBOT_SERVICE_TOKEN="s" * 32, OPENROUTER_API_KEY="test-only-key",
                    DATABASE_URL="postgresql://test:test@localhost/scholarbot_test", **overrides)


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
    def __init__(self, result=None, exists=True):
        self.exists = exists
        self.calls = []
        self.result = result or ToolResult(content='{"profile":true}', success=True, sources=[],
                                         usage=Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0))

    async def user_exists(self, user_id):
        return self.exists

    async def execute(self, *args):
        self.calls.append(args)
        return self.result


def chat_request():
    return ChatRequest(message="beasiswa", history=[], userId="stored-account-not-in-prompt")


async def test_native_calls_on_stop_consume_nested_budget_without_exposing_identity():
    provider = ScriptedProvider([completion([native()]), completion()])
    tools = RecordedTools(ToolResult(content='{"matched":true}', success=True, sources=[],
                                    usage=Usage(inputTokens=100, outputTokens=20, apiCalls=2, costUsd=None)))
    result = await ScholarRuntime(settings(), provider, tools).chat(chat_request())
    assert result["usage"] == {"inputTokens": 120, "outputTokens": 30, "apiCalls": 4, "costUsd": None}
    assert "stored-account-not-in-prompt" not in repr(provider.messages)


async def test_unknown_stored_account_is_rejected_before_any_provider_call():
    provider = ScriptedProvider([])
    tools = RecordedTools(exists=False)
    with pytest.raises(ServiceError, match="Unauthorized"):
        await ScholarRuntime(settings(), provider, tools).chat(chat_request())
    assert provider.messages is None
    assert not tools.calls


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


@pytest.mark.parametrize("content,success", [("[]", True), ('{"error":{"code":"MATCH_FAILED"}}', False)])
async def test_normalized_or_failed_tools_still_charge_actual_nested_usage(content, success):
    source = Source(id="stored-scholarship", name="Beasiswa", registrationUrl=None, guideUrl=None,
                    deadline="2026-12-31T00:00:00+00:00")
    tools = RecordedTools(ToolResult(content=content, success=success, sources=[source],
                                    usage=Usage(inputTokens=100, outputTokens=20, apiCalls=1, costUsd=0.2)))
    provider = ScriptedProvider([completion([native()]), completion()])
    result = await ScholarRuntime(settings(), provider, tools).chat(chat_request())
    assert result["usage"] == {"inputTokens": 120, "outputTokens": 30, "apiCalls": 3, "costUsd": 0.4}
    assert result["toolCalls"][0]["success"] is False
    assert result["sources"] == []


async def test_successful_data_sources_are_deduplicated_without_model_metadata():
    source = Source(id="stored-scholarship", name="Beasiswa", registrationUrl="https://scholarship.example/apply",
                    guideUrl=None, deadline="2026-12-31T00:00:00+00:00")
    tools = RecordedTools(ToolResult(content='{"scholarship":"stored-scholarship"}', success=True, sources=[source],
                                    usage=Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0)))
    first = completion([native("first"), native("second")])
    final = completion()
    final["sources"] = [{"id": "fabricated"}]
    result = await ScholarRuntime(settings(), ScriptedProvider([first, final]), tools).chat(chat_request())
    assert result["sources"] == [source.model_dump()]


@pytest.mark.parametrize("phase", ["provider", "tool", "account"])
async def test_disconnect_cancels_provider_nested_data_or_account_lookup(phase):
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
        async def user_exists(self, user_id):
            if phase == "account":
                return await block()
            return True

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
