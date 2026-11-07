"""Fixed, bounded SELECT queries against the application's existing PostgreSQL schema."""
from datetime import datetime

import asyncpg

from .contracts import ServiceError
from .settings import Settings

# Fail closed rather than silently presenting an incomplete category catalogue.
MAX_CATEGORIES = 1000


class ScholarRepository:
    def __init__(self, pool: asyncpg.Pool):
        self.pool = pool

    @classmethod
    async def create(cls, settings: Settings) -> "ScholarRepository":
        try:
            pool = await asyncpg.create_pool(
                dsn=settings.database_url.get_secret_value(), min_size=1, max_size=5,
                timeout=min(settings.timeout_seconds, 15), command_timeout=15,
                server_settings={
                    "default_transaction_read_only": "on",
                    "statement_timeout": "15000",
                    "idle_in_transaction_session_timeout": "15000",
                    "search_path": "public",
                },
            )
        except Exception:
            # Never surface a connection exception containing the DSN or account.
            raise ServiceError("Unavailable") from None
        return cls(pool)

    async def close(self) -> None:
        await self.pool.close()

    async def user_exists(self, user_id: str) -> bool:
        try:
            async with self.pool.acquire() as connection:
                async with connection.transaction(readonly=True):
                    return await connection.fetchval(
                        'SELECT EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "Id" = $1)', user_id,
                    )
        except Exception:
            raise ServiceError("Unavailable") from None

    async def profile(self, user_id: str) -> tuple[list[dict], list[str]]:
        async with self.pool.acquire() as connection:
            async with connection.transaction(readonly=True, isolation="repeatable_read"):
                categories = await connection.fetch('''
                    SELECT scholarship_category_id AS id, name
                    FROM scholarship_categories
                    WHERE is_active AND NOT is_deleted
                    ORDER BY name, scholarship_category_id LIMIT 1001
                ''')
                saved = await connection.fetch('''
                    SELECT DISTINCT category_id FROM user_scholarship_preferences
                    WHERE user_id = $1 ORDER BY category_id LIMIT 1001
                ''', user_id)
        if len(categories) > MAX_CATEGORIES or len(saved) > MAX_CATEGORIES:
            raise ServiceError("InvalidResponse")
        return [dict(row) for row in categories], [row["category_id"] for row in saved]

    async def open_candidates(self, category_ids: list[str], now: datetime) -> tuple[list[dict], int]:
        async with self.pool.acquire() as connection:
            async with connection.transaction(readonly=True):
                rows = await connection.fetch('''
                    SELECT s.scholarship_id AS id, s.name,
                        left(s.description, 600) AS description, s.total_prize AS "totalPrize",
                        s.guide_url AS "guideUrl", s.registration_url AS "registrationUrl",
                        ARRAY(SELECT left(r, 350) FROM unnest(s.requirement[1:8]) AS r) AS requirement,
                        ARRAY(SELECT a.category_id FROM scholarship_category_assignments AS a
                              WHERE a.scholarship_id = s.scholarship_id
                              ORDER BY a.category_id LIMIT 1001) AS "categoryIds",
                        s.registration_start_at AS "registrationStartAt",
                        s.registration_end_at AS "registrationEndAt",
                        count(*) OVER () AS "openCandidateCount"
                    FROM scholarships AS s
                    WHERE s.is_eligible AND NOT s.is_deleted
                        AND s.registration_start_at <= $2 AND s.registration_end_at >= $2
                        AND EXISTS(SELECT 1 FROM scholarship_category_assignments AS a
                                   WHERE a.scholarship_id = s.scholarship_id
                                       AND a.category_id = ANY($1::varchar[]))
                    ORDER BY s.registration_end_at, s.scholarship_id LIMIT 10
                ''', category_ids, now)
        candidates = [dict(row) for row in rows]
        if any(len(row["categoryIds"]) > MAX_CATEGORIES for row in candidates):
            raise ServiceError("InvalidResponse")
        return candidates, rows[0]["openCandidateCount"] if rows else 0

    async def find_scholarships(self, name: str) -> list[dict]:
        async with self.pool.acquire() as connection:
            async with connection.transaction(readonly=True):
                rows = await connection.fetch('''
                    SELECT scholarship_id AS id, name FROM scholarships
                    WHERE is_eligible AND NOT is_deleted AND strpos(lower(name), lower($1::text)) > 0
                    ORDER BY name, scholarship_id LIMIT 6
                ''', name)
        return [dict(row) for row in rows]

    async def scholarship(self, scholarship_id: str) -> dict | None:
        async with self.pool.acquire() as connection:
            async with connection.transaction(readonly=True):
                row = await connection.fetchrow('''
                    SELECT s.scholarship_id AS id, s.name,
                        left(s.description, 2001) AS description, s.total_prize AS "totalPrize",
                        ARRAY(SELECT left(r, 501) FROM unnest(s.requirement[1:20]) AS r) AS requirement,
                        ARRAY(SELECT left(c, 150) FROM unnest(s.contact_person[1:5]) AS c) AS "contactPerson",
                        ARRAY(SELECT c.name FROM scholarship_category_assignments AS a
                              JOIN scholarship_categories AS c ON c.scholarship_category_id = a.category_id
                              WHERE a.scholarship_id = s.scholarship_id AND c.is_active AND NOT c.is_deleted
                              ORDER BY c.name, c.scholarship_category_id LIMIT 1001) AS categories,
                        s.registration_start_at AS "registrationStartAt",
                        s.registration_end_at AS "registrationEndAt",
                        s.registration_url AS "registrationUrl", s.guide_url AS "guideUrl",
                        cardinality(s.requirement) > 20 AS "requirementsTruncated"
                    FROM scholarships AS s
                    WHERE s.scholarship_id = $1 AND s.is_eligible AND NOT s.is_deleted LIMIT 1
                ''', scholarship_id)
        if row is not None and len(row["categories"]) > MAX_CATEGORIES:
            raise ServiceError("InvalidResponse")
        return dict(row) if row is not None else None
