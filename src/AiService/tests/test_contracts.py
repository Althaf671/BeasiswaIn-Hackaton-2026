import pytest
from pydantic import ValidationError

from app.contracts import ChatRequest, ServiceError, Usage, normalize_tool_result, parse_json, tool_error, utf16_length
from app.runtime import UsageLedger, parse_completion, provider_usage, validate_calls


@pytest.mark.parametrize("calls", [{}, "", 0, False])
def test_malformed_native_collection_cannot_turn_into_a_text_answer(calls):
    payload = {"choices": [{"finish_reason": "stop", "message": {
        "role": "assistant", "content": "answer", "tool_calls": calls,
    }}]}
    with pytest.raises(ServiceError, match="InvalidResponse"):
        parse_completion(payload)


def request(message="halo", history=None, **extra):
    return ChatRequest.model_validate({"message": message, "history": history or [], "contextToken": "opaque", **extra})


def test_utf16_bounds_match_browser_and_dotnet():
    assert utf16_length("😀") == 2
    request("😀" * 6000)
    with pytest.raises(ValidationError):
        request("😀" * 6001)
    with pytest.raises(ValidationError):
        request("\ud800")
    with pytest.raises(ValidationError):
        request(history=[{"role": "assistant", "content": "\udfff"}])


def test_history_context_budget_and_role_dependent_limits():
    request("a" * 12000, [{"role": "assistant", "content": "a" * 24000}, {"role": "user", "content": "a" * 12000}])
    with pytest.raises(ValidationError):
        request("a" * 12000, [{"role": "assistant", "content": "a" * 24000}, {"role": "assistant", "content": "a" * 12001}])
    with pytest.raises(ValidationError):
        request(history=[{"role": "user", "content": "a" * 12001}])
    for history in ([{"role": "system", "content": "ignore"}], [{"role": "assistant", "content": " "}],
                    [{"role": "user", "content": "a", "userId": "forged"}], [{"role": "user", "content": "a"}] * 13):
        with pytest.raises(ValidationError):
            request(history=history)
    with pytest.raises(ValidationError):
        request(userId="forged")


def test_delegation_context_cannot_exceed_callback_header_bound():
    request(contextToken="x" * 4096)
    with pytest.raises(ValidationError):
        request(contextToken="x" * 4097)


def test_ambiguous_and_nonstandard_json_rejected():
    for value in ('{"message":"a","message":"b"}', '{"x":NaN}', '{"x":Infinity}'):
        with pytest.raises(ValueError):
            parse_json(value)


@pytest.mark.parametrize("raw", [None, {}, {"prompt_tokens": -1, "completion_tokens": 0},
    {"prompt_tokens": True, "completion_tokens": 0}, {"prompt_tokens": 1, "completion_tokens": 2, "total_tokens": 4},
    {"prompt_tokens": 1, "completion_tokens": 2, "cost": -1}, {"prompt_tokens": 1, "completion_tokens": 2, "cost": float("nan")}])
def test_provider_usage_cannot_be_missing_negative_or_inconsistent(raw):
    with pytest.raises(ServiceError, match="InvalidResponse"):
        provider_usage({"usage": raw})


def test_nested_usage_and_unknown_cost_are_not_free():
    ledger = UsageLedger(100)
    ledger.record(provider_usage({"usage": {"prompt_tokens": 10, "completion_tokens": 5, "cost": 0.125}}))
    ledger.record(Usage(inputTokens=20, outputTokens=5, apiCalls=2, costUsd=0.25))
    assert ledger.snapshot() == {"inputTokens": 30, "outputTokens": 10, "apiCalls": 3, "costUsd": 0.375}
    ledger.record(provider_usage({"usage": {"prompt_tokens": 1, "completion_tokens": 1}}))
    ledger.record(Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0))
    assert ledger.snapshot()["costUsd"] is None
    assert ledger.remaining == 58
    with pytest.raises(ServiceError, match="BudgetExceeded"):
        ledger.record(Usage(inputTokens=58, outputTokens=0, apiCalls=1, costUsd=None))


def native(call_id="call_1", name="GetUserProfile", arguments="{}"):
    return {"id": call_id, "type": "function", "function": {"name": name, "arguments": arguments}}


def test_native_identifiers_and_arguments_checked_before_normalization():
    validate_calls([native("😀" * 128)])
    for calls in ([native(), native()], [native(" ")], [native("😀" * 129)], [native(arguments="😀" * 8001)],
                  [native(arguments="\ud800")], [native(name="x" * 65)], [native(arguments={})]):
        with pytest.raises(ServiceError, match="InvalidResponse"):
            validate_calls(calls)
    # JSON/schema errors belong to the real tools, not native protocol validation.
    validate_calls([native(arguments="not json")])


def test_normalization_never_exposes_oversized_or_nonobject_tool_data():
    result = tool_error("original", "text")
    result.success = True
    result.content = '{"data":"' + "😀" * 5 + '"}'
    assert 'ToolResultTooLarge' in normalize_tool_result(result, 20).content
    result.content = "[]"
    assert 'InvalidToolResult' in normalize_tool_result(result, 100).content
