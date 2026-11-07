import asyncio
import hmac
import logging
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, Request, Security
from fastapi.exceptions import RequestValidationError
from fastapi.openapi.utils import get_openapi
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer
from openai import AsyncOpenAI
from pydantic import ValidationError
from starlette.exceptions import HTTPException
from starlette.responses import JSONResponse

from .contracts import (
    ERRORS, MAX_BODY_BYTES, MAX_RESPONSE_BYTES, ChatRequest, ChatResponse,
    ErrorResponse, HealthResponse, ServiceError, parse_json,
)
from .database import ScholarRepository
from .provider import OpenRouterProvider
from .runtime import ScholarRuntime
from .settings import Settings
from .tools import TOOL_DEFINITIONS, DataTools


def error_response(code: str) -> JSONResponse:
    error = ServiceError(code)
    return JSONResponse(error.envelope(), status_code=error.status, headers={"Cache-Control": "no-store"})


class AuthenticatedBodyLimit:
    """Authenticate before reading; bound actual chunks, not just Content-Length."""

    def __init__(self, app, settings: Settings | None = None):
        self.app = app
        self.settings = settings

    async def __call__(self, scope, receive, send):
        if scope["type"] != "http" or scope["path"] != "/scholarbot/chat":
            await self.app(scope, receive, send)
            return
        settings = self.settings or scope["app"].state.settings
        expected = b"Bearer " + settings.service_token.get_secret_value().encode("ascii")
        authorizations = [value for name, value in scope["headers"] if name.lower() == b"authorization"]
        supplied = authorizations[0] if len(authorizations) == 1 else b""
        authenticated = hmac.compare_digest(supplied, expected)
        if not authenticated:
            await error_response("Unauthorized")(scope, receive, send)
            return
        lengths = [value for name, value in scope["headers"] if name.lower() == b"content-length"]
        if lengths and (len(lengths) != 1 or not lengths[0].isdigit() or len(lengths[0]) > 10 or int(lengths[0]) > MAX_BODY_BYTES):
            await error_response("InvalidRequest")(scope, receive, send)
            return
        body = bytearray()
        try:
            async with asyncio.timeout(settings.timeout_seconds):
                while True:
                    event = await receive()
                    if event["type"] == "http.disconnect":
                        return
                    if event["type"] != "http.request":
                        await error_response("InvalidRequest")(scope, receive, send)
                        return
                    chunk = event.get("body", b"")
                    if len(chunk) > MAX_BODY_BYTES - len(body):
                        await error_response("InvalidRequest")(scope, receive, send)
                        return
                    body.extend(chunk)
                    if not event.get("more_body", False):
                        break
        except TimeoutError:
            await error_response("Timeout")(scope, receive, send)
            return
        delivered = False

        async def replay():
            nonlocal delivered
            if not delivered:
                delivered = True
                return {"type": "http.request", "body": bytes(body), "more_body": False}
            return await receive()

        await self.app(scope, replay, send)


async def until_disconnect(work, receive):
    async def watch():
        while True:
            if (await receive())["type"] == "http.disconnect":
                return

    running = asyncio.create_task(work)
    disconnected = asyncio.create_task(watch())
    try:
        done, _ = await asyncio.wait({running, disconnected}, return_when=asyncio.FIRST_COMPLETED)
        if disconnected in done:
            raise asyncio.CancelledError
        return await running
    finally:
        for task in (running, disconnected):
            if not task.done():
                task.cancel()
        await asyncio.gather(running, disconnected, return_exceptions=True)


def create_app() -> FastAPI:
    @asynccontextmanager
    async def lifespan(api: FastAPI):
        settings = Settings()
        # SDK debug logging can contain payloads; never enable it for this service.
        logging.getLogger("openai").setLevel(logging.WARNING)
        logging.getLogger("httpx").setLevel(logging.WARNING)
        timeout = httpx.Timeout(settings.timeout_seconds, connect=10.0)
        repository = await ScholarRepository.create(settings)
        try:
            async with httpx.AsyncClient(timeout=timeout, follow_redirects=False, trust_env=False) as provider_http:
                async with AsyncOpenAI(api_key=settings.openrouter_api_key.get_secret_value(),
                                       base_url=settings.openrouter_base_url, max_retries=0,
                                       timeout=timeout, http_client=provider_http) as provider_client:
                    provider = OpenRouterProvider(settings, provider_client, TOOL_DEFINITIONS)
                    api.state.settings = settings
                    api.state.runtime = ScholarRuntime(settings, provider, DataTools(settings, repository, provider))
                    yield
        finally:
            await repository.close()

    api = FastAPI(
        title="ScholarBot REST API",
        version="1.0.0",
        description=(
            "Scholarship-only assistant with read-only access to stored scholarship data. "
            "Call from a trusted authenticated backend, never directly from a browser. "
            "The backend must derive userId from its authenticated server principal. "
            "The service Bearer token authorizes that backend; it is not an end-user token. "
            "No database writes are available."
        ),
        lifespan=lifespan,
    )

    api.add_middleware(AuthenticatedBodyLimit)

    @api.exception_handler(HTTPException)
    async def safe_http_error(_request, _error):
        return error_response("InvalidRequest")

    @api.exception_handler(RequestValidationError)
    async def safe_validation_error(_request, _error):
        return error_response("InvalidRequest")

    @api.get("/health", response_model=HealthResponse, summary="Public process liveness", description="Does not check database or provider availability.")
    async def health():
        return {"status": "ok"}

    bearer = HTTPBearer(
        auto_error=False,
        scheme_name="ServiceBearer",
        description="Backend-only SCHOLARBOT_SERVICE_TOKEN; at least 32 URL-safe ASCII characters. Never expose it to the browser.",
    )
    error_responses = {}
    for code, (status, message) in ERRORS.items():
        response = error_responses.setdefault(status, {
            "model": ErrorResponse,
            "description": "",
            "content": {"application/json": {"examples": {}}},
        })
        response["description"] += f"{code}: {message} "
        response["content"]["application/json"]["examples"][code] = {"value": {"error": {"code": code, "message": message}}}

    @api.post(
        "/scholarbot/chat", response_model=ChatResponse, responses=error_responses,
        summary="Answer a scholarship question",
        description=(
            "Requires an authenticated backend-supplied stored userId; an unknown account fails before any AI call. "
            "Only message, history and userId are accepted. Tool arguments cannot select account identity. "
            "Request body is capped at 262,144 bytes; authentication runs before body consumption. "
            "Responses include actual provider and nested matcher usage; costUsd is null when any cost is unknown. "
            "All error responses use the documented safe envelope."
        ),
        openapi_extra={"requestBody": {
            "required": True,
            "content": {"application/json": {"schema": {"$ref": "#/components/schemas/ChatRequest"}}},
        }},
    )
    async def chat(
        request: Request,
        _credentials: HTTPAuthorizationCredentials | None = Security(bearer),
    ):
        try:
            try:
                # Parse manually so duplicate keys, invalid constants and excessive
                # nesting all receive the same safe validation error.
                value = ChatRequest.model_validate(parse_json(await request.body()))
            except (ValidationError, ValueError, UnicodeError, RecursionError):
                return error_response("InvalidRequest")
            try:
                async with asyncio.timeout(api.state.settings.timeout_seconds):
                    result = await until_disconnect(api.state.runtime.chat(value), request.receive)
            except TimeoutError:
                return error_response("Timeout")
            response = JSONResponse(result, headers={"Cache-Control": "no-store"})
            if len(response.body) > MAX_RESPONSE_BYTES:
                return error_response("InvalidResponse")
            return response
        except ServiceError as error:
            return error_response(error.code)
        except asyncio.CancelledError:
            raise
        except Exception:
            # Never serialize/log provider exceptions, request data or identity.
            return error_response("Unavailable")

    def documented_openapi():
        if api.openapi_schema is None:
            schema = get_openapi(title=api.title, version=api.version, description=api.description, routes=api.routes)
            request_schema = ChatRequest.model_json_schema(ref_template="#/components/schemas/{model}")
            schema["components"]["schemas"].update(request_schema.pop("$defs", {}))
            schema["components"]["schemas"]["ChatRequest"] = request_schema
            api.openapi_schema = schema
        return api.openapi_schema

    api.openapi = documented_openapi

    return api


app = create_app()
