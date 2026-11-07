import asyncio
import copy
import json
from datetime import datetime, timedelta, timezone
from decimal import Decimal

import pytest

from app.contracts import ChatRequest, ServiceError, parse_json, utf16_length
from app.runtime import ScholarRuntime
from app.settings import Settings
from app.tools import DataTools


NOW = datetime(2026, 11, 7, 12, tzinfo=timezone.utc)


@pytest.fixture(autouse=True)
def fixed_clock(monkeypatch):
    class Clock(datetime):
        @classmethod
        def now(cls, tz=None):
            return NOW.astimezone(tz) if tz is not None else NOW.replace(tzinfo=None)

    monkeypatch.setattr("app.tools.datetime", Clock)


def settings():
    return Settings(SCHOLARBOT_SERVICE_TOKEN="s" * 32, OPENROUTER_API_KEY="test-key",
                    DATABASE_URL="postgresql://fixture:fixture@localhost/fixture")


def scholarship(scholarship_id="one", **changes):
    row = {
        "id": scholarship_id, "name": "Authoritative database name", "description": "Source description",
        "totalPrize": Decimal("1000000"), "categoryIds": ["science"], "categories": ["Science"],
        "guideUrl": "https://example.org/guide", "registrationUrl": "https://example.org/apply",
        "requirement": ["Source requirement"], "contactPerson": ["Source contact"],
        "registrationStartAt": NOW - timedelta(days=1), "registrationEndAt": NOW + timedelta(days=1),
        "requirementsTruncated": False,
    }
    row.update(changes)
    return row


class Catalogue:
    """Small domain fixture, not a SQL interpreter or transport echo."""
    def __init__(self):
        self.categories = [{"id": "science", "name": "Science"}, {"id": "arts", "name": "Arts"}]
        self.saved = {"alice": ["science"], "bob": ["arts"]}
        self.rows = [scholarship()]
        self.total = 1

    async def user_exists(self, user_id):
        await asyncio.sleep(0)
        return user_id in self.saved

    async def profile(self, user_id):
        await asyncio.sleep(0)
        return copy.deepcopy(self.categories), self.saved.get(user_id, [])[:]

    async def open_candidates(self, category_ids, now):
        return copy.deepcopy(self.rows), self.total

    async def find_scholarships(self, name):
        return [{"id": row["id"], "name": row["name"]} for row in self.rows
                if name.casefold() in row["name"].casefold()][:6]

    async def scholarship(self, scholarship_id):
        return next((copy.deepcopy(row) for row in self.rows if row["id"] == scholarship_id), None)


def ranking(scholarship_id="one", **changes):
    result = {"opportunityId": scholarship_id, "opportunityName": "Invented model name",
              "matchReason": "Interest aligns", "feedback": "Read the guide", "matchScore": 82.5,
              "matchScoreSource": "Category and essay alignment"}
    result.update(changes)
    return result


class Matcher:
    def __init__(self, rows=None, content=None, **changes):
        self.payload = {"choices": [{"message": {"role": "assistant", "content": content if content is not None
                                                else json.dumps({"recommendations": rows if rows is not None else [ranking()]})},
                                     "finish_reason": "stop"}],
                        "usage": {"prompt_tokens": 100, "completion_tokens": 30, "cost": 0.02}}
        self.payload.update(changes)
        self.called = False
        self.messages = None

    async def complete_matching(self, messages, max_output_tokens):
        self.called = True
        self.messages = copy.deepcopy(messages)
        return copy.deepcopy(self.payload)


async def run(name, arguments, repository=None, provider=None, user_id="alice", remaining=10000, maximum=1000):
    tools = DataTools(settings(), repository or Catalogue(), provider or Matcher())
    return await tools.execute(name, arguments if isinstance(arguments, str) else json.dumps(arguments),
                               user_id, remaining, maximum)


async def test_profiles_are_account_scoped_and_forged_identity_cannot_select_another_account():
    tools = DataTools(settings(), Catalogue(), Matcher())
    alice, bob = await asyncio.gather(
        tools.execute("GetUserProfile", "{}", "alice", 10000, 1000),
        tools.execute("GetUserProfile", "{}", "bob", 10000, 1000),
    )
    assert parse_json(alice.content)["savedPreferences"] == [{"id": "science", "name": "Science"}]
    assert parse_json(bob.content)["savedPreferences"] == [{"id": "arts", "name": "Arts"}]
    assert parse_json(alice.content)["academicProfileAvailable"] is False
    assert parse_json(alice.content)["certificateHistoryAvailable"] is False
    forged = await tools.execute("GetUserProfile", '{"userId":"bob"}', "alice", 10000, 1000)
    unknown = await tools.execute("GetUserProfile", "{}", "missing", 10000, 1000)
    assert parse_json(forged.content)["error"]["code"] == "INVALID_ARGUMENTS"
    assert parse_json(unknown.content)["error"]["code"] == "UNAUTHORIZED"
    assert not forged.sources and not unknown.sources


@pytest.mark.parametrize("name,arguments", [
    ("GetUserProfile", '{"x":1,"x":2}'),
    ("GetUserProfile", "[]"),
    ("GetUserProfile", '{"x":' + '[' * 8 + '0' + ']' * 8 + '}'),
    ("GetRecommendations", {"essay": "a" * 2001}),
    ("GetRecommendations", {"essay": "😀" * 1001}),
    ("GetRecommendations", {"essay": "stated context", "limit": True}),
    ("GetRecommendations", {"essay": "stated context", "limit": 1.0}),
    ("GetRecommendations", {"essay": "stated context", "limit": 6}),
    ("GetRecommendations", {"essay": "stated context", "categoryIds": ["science"] * 16}),
    ("GetRecommendations", {"essay": "stated context", "categoryIds": [1]}),
    ("GetRecommendations", {"essay": "stated context", "categoryIds": [" "]}),
    ("GetRecommendations", {"essay": "stated context", "categoryIds": ["a" * 65]}),
    ("GetRecommendations", {"essay": "stated context", "userId": "bob"}),
    ("GetRecommendations", '{"essay":"\\ud800"}'),
    ("GetRecommendations", '{"essay":"context","limit":NaN}'),
    ("GetScholarshipDetails", {"scholarshipId": "one", "name": "name"}),
    ("GetScholarshipDetails", {"name": "ab"}),
    ("GetScholarshipDetails", {"scholarshipId": "a" * 65}),
    ("GetScholarshipDetails", {"name": "valid", "sql": "SELECT 1"}),
])
async def test_invalid_arguments_never_start_paid_matching(name, arguments):
    matcher = Matcher()
    result = await run(name, arguments, provider=matcher)
    assert not result.success
    assert parse_json(result.content)["error"]["code"] == "INVALID_ARGUMENTS"
    assert result.usage.apiCalls == 0 and not result.sources and not matcher.called


@pytest.mark.parametrize("categories,expected", [([], "PREFERENCES_REQUIRED"), (["missing"], "INVALID_CATEGORY")])
async def test_empty_or_unavailable_preferences_never_choose_arbitrary_categories(categories, expected):
    matcher = Matcher()
    result = await run("GetRecommendations", {"essay": "My stated context", "categoryIds": categories}, provider=matcher)
    assert parse_json(result.content)["error"]["code"] == expected
    assert not matcher.called


async def test_missing_saved_preferences_requires_user_choice_and_saved_inactive_ids_are_not_used():
    repository = Catalogue()
    repository.saved["alice"] = []
    result = await run("GetRecommendations", {"essay": "My context"}, repository=repository)
    assert parse_json(result.content)["error"]["code"] == "PREFERENCES_REQUIRED"
    repository.saved["alice"] = ["inactive"]
    result = await run("GetRecommendations", {"essay": "My context"}, repository=repository)
    assert parse_json(result.content)["error"]["code"] == "INVALID_CATEGORY"


async def test_ranking_uses_authoritative_fields_and_preserves_coverage_and_paid_usage():
    repository = Catalogue()
    repository.total = 14
    result = await run("GetRecommendations", {"essay": "I study science", "categoryIds": ["science", "science"]},
                       repository=repository)
    payload = parse_json(result.content)
    assert result.success and payload["coverageLimited"] and payload["openCandidateCount"] == 14
    assert payload["analyzedCandidateCount"] == 1
    assert payload["recommendations"][0]["opportunityName"] == repository.rows[0]["name"]
    assert payload["recommendations"][0]["registrationEndAt"] == repository.rows[0]["registrationEndAt"].isoformat()
    assert result.sources[0].id == "one" and result.sources[0].registrationUrl == "https://example.org/apply"
    assert result.usage.model_dump() == {"inputTokens": 100, "outputTokens": 30, "apiCalls": 1, "costUsd": 0.02}


@pytest.mark.parametrize("rows", [
    [ranking("foreign")], [ranking(), ranking()],
    [ranking(matchScore=True)], [ranking(matchScore=-1)], [ranking(matchScore=101)],
    [ranking(matchScore=float("inf"))], [ranking(matchScore="85")],
    [ranking(matchReason=" ")], [ranking(feedback="x" * 1601)],
    [ranking(matchScoreSource="")], [ranking(opportunityId=1)],
    [ranking(), ranking("foreign")],
    [ranking(), ranking("two", feedback=" ")],
    [ranking(str(number)) for number in range(6)],
])
async def test_invalid_ranking_rows_including_rows_after_limit_fail_without_sources_but_charge_usage(rows):
    repository = Catalogue()
    repository.rows.append(scholarship("two"))
    result = await run("GetRecommendations", {"essay": "My context", "limit": 1},
                       repository=repository, provider=Matcher(rows))
    assert not result.success and not result.sources
    assert parse_json(result.content)["error"]["code"] == "INVALID_MATCHING_OUTPUT"
    assert result.usage.apiCalls == 1 and result.usage.inputTokens == 100 and result.usage.outputTokens == 30


@pytest.mark.parametrize("content", ["not json", "[]", '{"recommendations":{}}',
                                     '{"recommendations":[],"recommendations":[]}',
                                     '{"recommendations":[{"matchScore":NaN}]}'])
async def test_invalid_matching_json_retains_nested_usage(content):
    result = await run("GetRecommendations", {"essay": "Context"}, provider=Matcher(content=content))
    assert not result.success and not result.sources and result.usage.apiCalls == 1


async def test_missing_usage_fails_safe_instead_of_inventing_free_matching():
    matcher = Matcher(usage=None)
    with pytest.raises(ServiceError, match="InvalidResponse"):
        await run("GetRecommendations", {"essay": "Context"}, provider=matcher)


@pytest.mark.parametrize("finish,refusal", [("length", None), ("content_filter", None), ("stop", "refused")])
async def test_incomplete_or_refused_matching_is_charged_but_never_published(finish, refusal):
    matcher = Matcher()
    matcher.payload["choices"][0]["finish_reason"] = finish
    matcher.payload["choices"][0]["message"]["refusal"] = refusal
    result = await run("GetRecommendations", {"essay": "Context"}, provider=matcher)
    assert not result.success and not result.sources and result.usage.apiCalls == 1


async def test_exceeded_matching_output_is_rejected_but_charged():
    result = await run("GetRecommendations", {"essay": "Context"}, maximum=20)
    assert not result.success and result.usage.outputTokens == 30
    assert result.usage.apiCalls == 1


async def test_normalized_oversize_ranking_still_counts_paid_matching_in_chat_usage():
    class Agent:
        def __init__(self):
            self.turn = 0

        async def complete(self, messages, max_output_tokens):
            self.turn += 1
            calls = [{"id": "rank", "type": "function", "function": {
                "name": "GetRecommendations", "arguments": '{"essay":"My context"}',
            }}] if self.turn == 1 else []
            return {"choices": [{"message": {"role": "assistant", "content": "Please narrow your question",
                                             "tool_calls": calls}, "finish_reason": "stop"}],
                    "usage": {"prompt_tokens": 10, "completion_tokens": 5, "cost": 0.01}}

    config = settings().model_copy(update={"max_tool_result_characters": 2000})
    tools = DataTools(config, Catalogue(), Matcher([ranking(feedback="x" * 1600)]))
    result = await ScholarRuntime(config, Agent(), tools).chat(ChatRequest(message="Find scholarships", history=[], userId="alice"))
    assert result["usage"] == {"inputTokens": 120, "outputTokens": 40, "apiCalls": 3, "costUsd": 0.04}
    assert not result["toolCalls"][0]["success"] and not result["sources"]


async def test_invalid_paid_ranking_can_exhaust_budget_without_another_agent_call():
    class Agent:
        def __init__(self):
            self.calls = 0

        async def complete(self, messages, max_output_tokens):
            self.calls += 1
            return {"choices": [{"message": {"role": "assistant", "content": None, "tool_calls": [
                {"id": "rank", "type": "function", "function": {
                    "name": "GetRecommendations", "arguments": '{"essay":"Context"}',
                }},
            ]}, "finish_reason": "tool_calls"}],
                "usage": {"prompt_tokens": 10, "completion_tokens": 5, "cost": 0}}

    config = settings().model_copy(update={"max_total_tokens": 4000, "max_output_tokens": 1000})
    agent = Agent()
    matcher = Matcher([ranking("foreign")], usage={"prompt_tokens": 3980, "completion_tokens": 30})
    tools = DataTools(config, Catalogue(), matcher)
    with pytest.raises(ServiceError, match="BudgetExceeded"):
        await ScholarRuntime(config, agent, tools).chat(ChatRequest(message="Find scholarships", history=[], userId="alice"))
    assert agent.calls == 1


async def test_no_open_candidates_does_not_call_matcher_or_claim_ineligibility():
    repository, matcher = Catalogue(), Matcher()
    repository.rows = []
    result = await run("GetRecommendations", {"essay": "Context"}, repository=repository, provider=matcher)
    assert result.success and parse_json(result.content)["status"] == "no_matches"
    assert not matcher.called and result.usage.apiCalls == 0 and not result.sources




async def test_detail_caps_source_urls_and_disambiguation_preserve_database_authority():
    repository = Catalogue()
    repository.rows[0].update(description="😀" * 1001, requirement=["😀" * 251] * 21,
                              contactPerson=["x" * 151] * 6, guideUrl="javascript:alert(1)",
                              registrationUrl="https://user:secret@example.org/apply", requirementsTruncated=True)
    result = await run("GetScholarshipDetails", {"scholarshipId": "one"}, repository=repository)
    payload = parse_json(result.content)
    assert result.success and payload["contentTruncated"]
    assert utf16_length(payload["description"]) == 2000
    assert len(payload["requirements"]) == 20 and all(utf16_length(item) == 500 for item in payload["requirements"])
    assert len(payload["contactPerson"]) == 5 and all(utf16_length(item) == 150 for item in payload["contactPerson"])
    assert payload["guideUrl"] is None and payload["registrationUrl"] is None
    assert result.sources[0].guideUrl is None and result.sources[0].registrationUrl is None
    repository.rows = [scholarship(str(number), name=f"Shared name {number}") for number in range(6)]
    choice = await run("GetScholarshipDetails", {"name": "shared name"}, repository=repository)
    assert parse_json(choice.content)["status"] == "choose_scholarship"
    assert len(parse_json(choice.content)["matches"]) == 5 and parse_json(choice.content)["hasMore"]
    assert not choice.sources


@pytest.mark.parametrize("offset,expected", [(-2, "closed"), (2, "upcoming"), (0, "open")])
async def test_detail_registration_status_uses_database_dates(offset, expected):
    repository = Catalogue()
    if offset < 0:
        repository.rows[0]["registrationEndAt"] = NOW + timedelta(days=offset)
    elif offset > 0:
        repository.rows[0]["registrationStartAt"] = NOW + timedelta(days=offset)
    result = await run("GetScholarshipDetails", {"scholarshipId": "one"}, repository=repository)
    assert parse_json(result.content)["registrationStatus"] == expected


async def test_matching_cancellation_is_not_normalized_as_service_failure():
    started, cancelled = asyncio.Event(), asyncio.Event()

    class BlockingMatcher:
        async def complete_matching(self, messages, max_output_tokens):
            started.set()
            try:
                await asyncio.Future()
            finally:
                cancelled.set()

    task = asyncio.create_task(run("GetRecommendations", {"essay": "Context"}, provider=BlockingMatcher()))
    await asyncio.wait_for(started.wait(), 1)
    task.cancel()
    with pytest.raises(asyncio.CancelledError):
        await task
    assert cancelled.is_set()


async def test_database_failures_do_not_leak_identity_or_connection_details():
    class BrokenCatalogue(Catalogue):
        async def profile(self, user_id):
            raise RuntimeError("postgresql://secret:password@host/database " + user_id)

    result = await run("GetUserProfile", {}, repository=BrokenCatalogue())
    assert not result.success and parse_json(result.content)["error"]["code"] == "SERVICE_UNAVAILABLE"
    assert "secret" not in result.content and "alice" not in result.content
