import asyncio
import hmac
import logging
from contextlib import asynccontextmanager

import httpx
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from openai import AsyncOpenAI
from pydantic import ValidationError
from starlette.exceptions import HTTPException
from starlette.responses import JSONResponse

from .contracts import MAX_BODY_BYTES, MAX_RESPONSE_BYTES, ChatRequest, ServiceError, parse_json
from .runtime import OpenRouterProvider, ScholarRuntime
from .settings import Settings
from .tools import DotnetTools


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
        async with httpx.AsyncClient(timeout=timeout, follow_redirects=False, trust_env=False) as provider_http:
            async with httpx.AsyncClient(timeout=timeout, follow_redirects=False, trust_env=False) as callback_http:
                async with AsyncOpenAI(api_key=settings.openrouter_api_key.get_secret_value(),
                                       base_url=settings.openrouter_base_url, max_retries=0,
                                       timeout=timeout, http_client=provider_http) as provider_client:
                    api.state.settings = settings
                    api.state.runtime = ScholarRuntime(settings, OpenRouterProvider(settings, provider_client),
                                                       DotnetTools(settings, callback_http))
                    yield

    api = FastAPI(lifespan=lifespan, docs_url=None, redoc_url=None, openapi_url=None)

    api.add_middleware(AuthenticatedBodyLimit)

    @api.exception_handler(HTTPException)
    async def safe_http_error(_request, _error):
        return error_response("InvalidRequest")

    @api.exception_handler(RequestValidationError)
    async def safe_validation_error(_request, _error):
        return error_response("InvalidRequest")

    @api.get("/health")
    async def health():
        return {"status": "ok"}

    @api.post("/scholarbot/chat")
    async def chat(request: Request):
        try:
            try:
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
            # Never serialize/log provider exceptions, request data or delegation.
            return error_response("Unavailable")

    return api


app = create_app()
