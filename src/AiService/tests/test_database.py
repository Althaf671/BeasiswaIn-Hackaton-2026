"""Opt-in integration checks using existing rows only; no Docker, DDL, or seeding.

Set SCHOLARBOT_TEST_DATABASE_URL to the existing forwarded PostgreSQL endpoint.
These checks never inspect account credential columns or print account identifiers.
"""
import os
from datetime import datetime, timezone

import pytest

from app.database import ScholarRepository
from app.settings import Settings


@pytest.fixture
async def repository():
    dsn = os.environ.get("SCHOLARBOT_TEST_DATABASE_URL")
    if not dsn:
        pytest.skip("SCHOLARBOT_TEST_DATABASE_URL is required for read-only PostgreSQL integration")
    settings = Settings(SCHOLARBOT_SERVICE_TOKEN="s" * 32, OPENROUTER_API_KEY="test-key", DATABASE_URL=dsn)
    instance = await ScholarRepository.create(settings)
    try:
        yield instance
    finally:
        await instance.close()


async def read(repository, query, *parameters):
    async with repository.pool.acquire() as connection:
        async with connection.transaction(readonly=True):
            return await connection.fetch(query, *parameters)


async def test_real_sessions_and_transactions_remain_readonly_after_pool_release(repository):
    for _ in range(2):
        async with repository.pool.acquire() as connection:
            async with connection.transaction(readonly=True):
                assert await connection.fetchval("SELECT current_setting('default_transaction_read_only')") == "on"
                assert await connection.fetchval("SELECT current_setting('transaction_read_only')") == "on"
                timeout = await connection.fetchval("SELECT setting::integer FROM pg_settings WHERE name = 'statement_timeout'")
                assert 0 < timeout <= 15000


async def test_real_existing_unknown_and_injected_accounts(repository):
    accounts = await read(repository, 'SELECT "Id" FROM "AspNetUsers" ORDER BY "Id" LIMIT 1')
    if not accounts:
        pytest.skip("Existing-account fixture is unavailable")
    assert await repository.user_exists(accounts[0]["Id"])
    assert not await repository.user_exists("scholarbot-missing-account-' OR true --")
    assert not await repository.user_exists("' OR '1'='1")


async def test_real_profile_preferences_are_scoped_to_each_existing_account(repository):
    accounts = await read(repository, 'SELECT "Id" FROM "AspNetUsers" ORDER BY "Id" LIMIT 2')
    if len(accounts) < 2:
        pytest.skip("Two existing accounts are required for database isolation coverage")
    for account in accounts:
        user_id = account["Id"]
        categories, saved = await repository.profile(user_id)
        stored_preferences = await read(repository, '''
            SELECT category_id FROM user_scholarship_preferences WHERE user_id = $1
        ''', user_id)
        assert set(saved) == {row["category_id"] for row in stored_preferences}
        for category in categories:
            status = await read(repository, '''
                SELECT is_active, is_deleted FROM scholarship_categories WHERE scholarship_category_id = $1
            ''', category["id"])
            assert status and status[0]["is_active"] and not status[0]["is_deleted"]


async def test_real_name_search_treats_wildcards_and_injection_as_literal_text(repository):
    for term in ("%", "_", "' OR true --", "Science"):
        matches = await repository.find_scholarships(term)
        assert len(matches) <= 6
        for match in matches:
            assert term.lower() in match["name"].lower()
            raw = await read(repository, '''
                SELECT name, is_eligible, is_deleted FROM scholarships WHERE scholarship_id = $1
            ''', match["id"])
            assert raw and raw[0]["is_eligible"] and not raw[0]["is_deleted"]


async def test_real_candidates_match_independent_visibility_category_and_inclusive_date_filter(repository):
    raw = await read(repository, '''
        SELECT s.scholarship_id AS id, s.registration_start_at AS starts,
            s.registration_end_at AS ends, s.is_eligible, s.is_deleted,
            ARRAY(SELECT a.category_id FROM scholarship_category_assignments AS a
                  WHERE a.scholarship_id = s.scholarship_id) AS categories
        FROM scholarships AS s ORDER BY s.scholarship_id LIMIT 2001
    ''')
    if len(raw) > 2000:
        pytest.skip("Independent fixture snapshot exceeds its safety bound")
    categories, _ = await repository.profile("scholarbot-no-such-fixture-account")
    visible = next((row for row in raw if row["is_eligible"] and not row["is_deleted"]
                    and any(category["id"] in row["categories"] for category in categories)), None)
    if visible is None:
        pytest.skip("No existing visible categorized scholarship is available")
    category_ids = [category["id"] for category in categories if category["id"] in visible["categories"]][:1]
    for now in (visible["starts"], visible["ends"], datetime.now(timezone.utc)):
        expected = sorted((row for row in raw if row["is_eligible"] and not row["is_deleted"]
                           and row["starts"] <= now <= row["ends"]
                           and set(category_ids).intersection(row["categories"])),
                          key=lambda row: (row["ends"], row["id"]))
        candidates, total = await repository.open_candidates(category_ids, now)
        assert total == len(expected)
        assert [row["id"] for row in candidates] == [row["id"] for row in expected[:10]]
        assert len(candidates) <= 10
        for row in candidates:
            assert len(row["description"]) <= 600 and len(row["requirement"]) <= 8
            assert all(len(requirement) <= 350 for requirement in row["requirement"])


async def test_real_detail_excludes_hidden_deleted_and_injected_ids(repository):
    rows = await read(repository, '''
        SELECT scholarship_id AS id, is_eligible, is_deleted FROM scholarships
        ORDER BY scholarship_id LIMIT 100
    ''')
    if not rows:
        pytest.skip("Existing scholarship fixture is unavailable")
    for row in rows:
        detail = await repository.scholarship(row["id"])
        if row["is_eligible"] and not row["is_deleted"]:
            assert detail is not None and detail["id"] == row["id"]
            assert len(detail["description"]) <= 2001 and len(detail["requirement"]) <= 20
            assert len(detail["contactPerson"]) <= 5
        else:
            assert detail is None
    assert await repository.scholarship("' OR true --") is None
