"""Read-only ScholarBot tools; request identity never comes from model arguments."""
import json
import math
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import urlsplit

from .contracts import ServiceError, Source, ToolResult, Usage, parse_json, tool_error, utf16_length
from .database import ScholarRepository
from .matching import encode_json, matching_messages
from .provider import OpenRouterProvider, provider_usage
from .settings import Settings

TOOL_DEFINITIONS = json.loads(Path(__file__).with_name("tool_definitions.json").read_text(encoding="utf-8"))
KNOWN_TOOLS = frozenset(item["function"]["name"] for item in TOOL_DEFINITIONS)


def _zero_usage() -> Usage:
    return Usage(inputTokens=0, outputTokens=0, apiCalls=0, costUsd=0)


def _failure(code: str, message: str, usage: Usage | None = None) -> ToolResult:
    result = tool_error(code, message)
    if usage is not None:
        result.usage = usage
    return result


def _success(content: dict, sources: list[Source] | None = None, usage: Usage | None = None) -> ToolResult:
    return ToolResult(content=encode_json(content), success=True, sources=sources or [],
                      usage=usage if usage is not None else _zero_usage())


def _string(args: dict, key: str, minimum: int, maximum: int) -> str:
    value = args.get(key)
    if not isinstance(value, str):
        raise ValueError("Invalid string")
    value = value.strip()
    if not minimum <= utf16_length(value) <= maximum:
        raise ValueError("Invalid string length")
    return value


def _depth(value, depth: int = 1) -> None:
    if isinstance(value, (dict, list)):
        if depth > 8:
            raise ValueError("Arguments too deep")
        for item in (value.values() if isinstance(value, dict) else value):
            _depth(item, depth + 1)
    elif isinstance(value, str):
        utf16_length(value)


def _limit(value: str, maximum: int) -> str:
    # C# limits count UTF-16 code units. Never emit a dangling surrogate.
    return value.encode("utf-16-le")[:maximum * 2].decode("utf-16-le", errors="ignore")


def _safe_url(value: str) -> str | None:
    try:
        parsed = urlsplit(value)
        if (utf16_length(value) <= 4096 and parsed.scheme in {"http", "https"}
                and parsed.hostname and not parsed.username and not parsed.password
                and not any(ord(c) <= 32 for c in value)):
            parsed.port  # Reject malformed ports as well as malformed hosts.
            return value
    except (ValueError, UnicodeError):
        pass
    return None


def _source(scholarship: dict) -> Source:
    return Source(id=scholarship["id"], name=scholarship["name"],
                  registrationUrl=_safe_url(scholarship["registrationUrl"]),
                  guideUrl=_safe_url(scholarship["guideUrl"]),
                  deadline=scholarship["registrationEndAt"].isoformat())


class DataTools:
    def __init__(self, settings: Settings, repository: ScholarRepository, provider: OpenRouterProvider):
        self.settings = settings
        self.repository = repository
        self.provider = provider

    async def user_exists(self, user_id: str) -> bool:
        return await self.repository.user_exists(user_id)

    async def execute(self, name: str, arguments: str, user_id: str,
                      remaining_tokens: int, max_output_tokens: int) -> ToolResult:
        if not user_id or not user_id.strip():
            return _failure("UNAUTHORIZED", "Silakan masuk untuk membaca data akun Anda.")
        if name not in KNOWN_TOOLS:
            return _failure("TOOL_NOT_ALLOWED", "Tool tersebut tidak tersedia. ScholarBot hanya memiliki akses baca.")
        try:
            if not arguments or utf16_length(arguments) > 8000:
                raise ValueError("Invalid argument length")
            args = parse_json(arguments)
            if not isinstance(args, dict):
                raise ValueError("Arguments must be an object")
            _depth(args)
        except (ValueError, UnicodeError, RecursionError):
            return _failure("INVALID_ARGUMENTS", "Argumen harus berupa objek JSON valid, tanpa duplikasi, dengan panjang dan kedalaman terbatas.")
        try:
            if name == "GetUserProfile":
                if args:
                    return _failure("INVALID_ARGUMENTS", "GetUserProfile tidak menerima parameter atau userId.")
                return await self._profile(user_id)
            if name == "GetRecommendations":
                return await self._recommendations(args, user_id, remaining_tokens, max_output_tokens)
            return await self._details(args)
        except ServiceError:
            raise
        except Exception:
            # No exception text, query, credential, or request identity is exposed.
            return _failure("SERVICE_UNAVAILABLE", "Data belum dapat diambil dari layanan. Jangan menyimpulkan hasil yang belum tersedia.")

    async def _profile(self, user_id: str) -> ToolResult:
        if not await self.user_exists(user_id):
            return _failure("UNAUTHORIZED", "Akun untuk sesi ini tidak ditemukan. Silakan masuk kembali.")
        available, saved = await self.repository.profile(user_id)
        saved_ids = set(saved)
        return _success({
            "authenticated": True,
            "savedPreferences": [category for category in available if category["id"] in saved_ids],
            "availableCategories": available,
            "academicProfileAvailable": False,
            "certificateHistoryAvailable": False,
            "limitations": [
                "Data akun yang tersedia untuk konsultasi hanya preferensi kategori tersimpan.",
                "Essay, IPK, jurusan, dan sertifikat onboarding tidak tersedia sebagai profil tersimpan. Fakta dalam chat hanya konteks percakapan, bukan data yang sudah disimpan.",
            ],
        })

    async def _recommendations(self, args: dict, user_id: str,
                               remaining_tokens: int, max_output_tokens: int) -> ToolResult:
        try:
            if args.keys() - {"essay", "categoryIds", "limit"}:
                raise ValueError("Unknown arguments")
            essay = _string(args, "essay", 1, 2000)
            limit = args.get("limit", 5)
            if type(limit) is not int or not 1 <= limit <= 5:
                raise ValueError("Invalid limit")
            category_ids = None
            if "categoryIds" in args:
                raw_ids = args["categoryIds"]
                if not isinstance(raw_ids, list) or len(raw_ids) > 15:
                    raise ValueError("Invalid categories")
                category_ids = []
                for value in raw_ids:
                    if not isinstance(value, str) or not value.strip() or utf16_length(value) > 64:
                        raise ValueError("Invalid category ID")
                    value = value.strip()
                    if value not in category_ids:
                        category_ids.append(value)
        except (ValueError, UnicodeError):
            return _failure("INVALID_ARGUMENTS", "Sertakan essay 1–2.000 karakter, maksimal 15 ID kategori valid, dan limit bilangan bulat antara 1 dan 5; jangan mengarang profil.")
        available, saved = await self.repository.profile(user_id)
        if category_ids is None:
            category_ids = saved
        if not category_ids:
            return _failure("PREFERENCES_REQUIRED", "Belum ada kategori untuk pencarian. Tanyakan minat pengguna lalu gunakan kategori valid dari GetUserProfile; jangan memilih secara acak.")
        selected = [category for category in available if category["id"] in category_ids]
        if len(selected) != len(category_ids):
            return _failure("INVALID_CATEGORY", "Ada kategori yang tidak aktif atau tidak ditemukan. Baca ulang kategori yang tersedia.")
        now = datetime.now(timezone.utc)
        candidates, total = await self.repository.open_candidates(category_ids, now)
        if not candidates:
            return _success({"status": "no_matches", "recommendations": [],
                             "selectionBasis": "kategori dan periode pendaftaran", "checkedAt": now,
                             "message": "Tidak ada beasiswa yang sedang buka untuk kategori tersebut. Ini bukan kesimpulan bahwa pengguna tidak memenuhi syarat."})
        considered = [{
            "id": row["id"], "name": row["name"], "description": _limit(row["description"], 600),
            "totalPrize": row["totalPrize"], "contentImageUrl": "", "contactPerson": [],
            "categoryIds": row["categoryIds"], "guideUrl": _safe_url(row["guideUrl"]),
            "registrationUrl": _safe_url(row["registrationUrl"]),
            "requirement": [_limit(requirement, 350) for requirement in row["requirement"][:8]],
            "registrationStartAt": row["registrationStartAt"], "registrationEndAt": row["registrationEndAt"],
        } for row in candidates]
        output_limit = min(remaining_tokens, max_output_tokens)
        if output_limit <= 0:
            raise ServiceError("BudgetExceeded")
        payload = await self.provider.complete_matching(
            matching_messages(essay, [category["name"] for category in selected], considered), output_limit,
        )
        # Account for the paid call before examining any generated ranking fields.
        usage = provider_usage(payload)
        try:
            if usage.outputTokens > output_limit:
                raise ValueError("Provider exceeded output cap")
            choices = payload.get("choices")
            if not isinstance(choices, list) or len(choices) != 1 or not isinstance(choices[0], dict):
                raise ValueError("Invalid matching choices")
            choice = choices[0]
            message = choice.get("message")
            if (choice.get("finish_reason") != "stop" or not isinstance(message, dict)
                    or message.get("role") != "assistant" or message.get("tool_calls")
                    or message.get("refusal")):
                raise ValueError("Incomplete matching output")
            content = message.get("content")
            if not isinstance(content, str) or utf16_length(content) > 24000:
                raise ValueError("Invalid matching content")
            content = content.strip()
            if content.lower().startswith("```json") and content.endswith("```"):
                content = content[7:-3].strip()
            output = parse_json(content)
            if not isinstance(output, dict):
                raise ValueError("Invalid ranking object")
            ranking = output.get("recommendations")
            if not isinstance(ranking, list) or len(ranking) > 5:
                raise ValueError("Invalid ranking list")
            by_id = {row["id"]: row for row in candidates}
            seen, results, sources = set(), [], []
            for item in ranking:
                if not isinstance(item, dict):
                    raise ValueError("Invalid ranking row")
                scholarship_id = _string(item, "opportunityId", 1, 64)
                reason = _string(item, "matchReason", 1, 1600)
                feedback = _string(item, "feedback", 1, 1600)
                score_source = _string(item, "matchScoreSource", 1, 1600)
                score = item.get("matchScore")
                if (scholarship_id in seen or scholarship_id not in by_id
                        or type(score) not in {int, float} or not math.isfinite(score) or not 0 <= score <= 100):
                    raise ValueError("Non-authoritative ranking or invalid score")
                seen.add(scholarship_id)
                # Validate ALL generated rows, including those beyond the requested limit.
                if len(results) >= limit:
                    continue
                candidate = by_id[scholarship_id]
                results.append({"opportunityId": candidate["id"], "opportunityName": candidate["name"],
                                "matchReason": reason, "feedback": feedback, "matchScore": score,
                                "matchScoreSource": score_source,
                                "registrationStartAt": candidate["registrationStartAt"],
                                "registrationEndAt": candidate["registrationEndAt"], "registrationStatus": "open"})
                sources.append(_source(candidate))
            return _success({
                "status": "ok" if results else "no_matches", "recommendations": results,
                "selectionBasis": "kategori + analisis AI existing, bukan pemeriksaan kelayakan deterministik",
                "checkedAt": now, "openCandidateCount": total, "analyzedCandidateCount": len(considered),
                "candidateSelection": "maksimal 10 kandidat dengan deadline terdekat", "coverageLimited": total > 10,
                "limitations": [
                    "Skor merupakan estimasi AI, bukan persentase peluang diterima atau jaminan memenuhi syarat.",
                    "Tidak ada sertifikat tersimpan yang dibaca; deskripsi dan persyaratan kandidat dibatasi untuk konteks AI. Baca GetScholarshipDetails sebelum menyusun checklist.",
                ],
            }, sources, usage)
        except (ValueError, TypeError, KeyError, UnicodeError, RecursionError, OverflowError):
            return _failure("INVALID_MATCHING_OUTPUT", "Keluaran matching belum dapat divalidasi terhadap kandidat database.", usage)

    async def _details(self, args: dict) -> ToolResult:
        try:
            if args.keys() - {"scholarshipId", "name"} or ("scholarshipId" in args) == ("name" in args):
                raise ValueError("Use exactly one ID or name")
            scholarship_id = _string(args, "scholarshipId", 1, 64) if "scholarshipId" in args else None
            name = _string(args, "name", 3, 160) if "name" in args else None
        except (ValueError, UnicodeError):
            return _failure("INVALID_ARGUMENTS", "Sertakan tepat satu scholarshipId (1–64 karakter) atau name (3–160 karakter), bukan parameter lain.")
        if name is not None:
            matches = await self.repository.find_scholarships(name)
            if not matches:
                return _failure("NOT_FOUND", "Beasiswa tidak ditemukan dalam katalog yang tersedia.")
            if len(matches) > 1:
                return _success({"status": "choose_scholarship", "matches": matches[:5], "hasMore": len(matches) > 5})
            scholarship_id = matches[0]["id"]
        scholarship = await self.repository.scholarship(scholarship_id)
        if scholarship is None:
            return _failure("NOT_FOUND", "Beasiswa tidak ditemukan dalam katalog yang tersedia.")
        now = datetime.now(timezone.utc)
        start, end = scholarship["registrationStartAt"], scholarship["registrationEndAt"]
        return _success({
            "id": scholarship["id"], "name": scholarship["name"],
            "description": _limit(scholarship["description"], 2000), "totalPrize": scholarship["totalPrize"],
            "requirements": [_limit(value, 500) for value in scholarship["requirement"][:20]],
            "contactPerson": [_limit(value, 150) for value in scholarship["contactPerson"][:5]],
            "categories": scholarship["categories"], "registrationStartAt": start, "registrationEndAt": end,
            "registrationStatus": "closed" if end < now else "upcoming" if start > now else "open",
            "registrationUrl": _safe_url(scholarship["registrationUrl"]), "guideUrl": _safe_url(scholarship["guideUrl"]),
            "checkedAt": now,
            "contentTruncated": (utf16_length(scholarship["description"]) > 2000
                                 or scholarship["requirementsTruncated"]
                                 or any(utf16_length(value) > 500 for value in scholarship["requirement"])),
            "limitation": "Persyaratan merupakan informasi sumber, bukan hasil pemeriksaan profil pengguna. Gunakan tautan panduan untuk dokumen lengkap.",
        }, [_source(scholarship)])
