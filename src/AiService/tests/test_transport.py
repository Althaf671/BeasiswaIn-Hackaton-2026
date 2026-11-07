import asyncio
import json

import httpx
import pytest
from openai import AsyncOpenAI

from app.contracts import MAX_BODY_BYTES, ServiceError
from app.main import AuthenticatedBodyLimit
from app.runtime import OpenRouterProvider
from app.settings import Settings
from app.tools import DotnetTools


def settings():
    return Settings(SCHOLARBOT_SERVICE_TOKEN="s" * 32, OPENROUTER_API_KEY="test-only-key")


async def invoke_guard(chunks, headers):
    received = 0
    downstream = False
    output = []
    iterator = iter(chunks)

    async def receive():
        nonlocal received
        received += 1
        return next(iterator)

    async def send(event):
        output.append(event)

    async def app(scope, receive, send):
        nonlocal downstream
        downstream = True

    guard = AuthenticatedBodyLimit(app, settings())
    await guard({"type": "http", "path": "/scholarbot/chat", "headers": headers}, receive, send)
    return received, downstream, output


async def test_auth_precedes_body_reads_even_when_oversized_or_chunked():
    count, called, output = await invoke_guard([], [(b"authorization", b"Bearer wrong"), (b"content-length", b"999999")])
    assert count == 0 and not called
    assert output[0]["status"] == 401
    assert json.loads(output[1]["body"])["error"]["code"] == "Unauthorized"


async def test_actual_chunked_bytes_are_bounded_without_content_length():
    chunks = [{"type": "http.request", "body": b"x" * (MAX_BODY_BYTES - 1), "more_body": True},
              {"type": "http.request", "body": b"xx", "more_body": True}]
    count, called, output = await invoke_guard(chunks, [(b"authorization", b"Bearer " + b"s" * 32)])
    assert count == 2 and not called
    assert output[0]["status"] == 400
    assert b"InvalidRequest" in output[1]["body"]


async def test_declared_oversize_rejected_without_consuming_body():
    count, called, output = await invoke_guard([], [(b"authorization", b"Bearer " + b"s" * 32), (b"content-length", b"262145")])
    assert count == 0 and not called and output[0]["status"] == 400


async def test_unknown_or_oversized_tool_requests_never_reach_data_backend():
    calls = []

    async def handler(request):
        calls.append(request)
        raise AssertionError("Rejected tool requests must not reach the data backend")

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        tools = DotnetTools(settings(), client)
        unknown = await tools.execute("https://evil.example", '{}', "opaque", 100, 50)
        assert not unknown.success
        assert json.loads(unknown.content)["error"]["code"] == "UnknownTool"
        oversize = await tools.execute("GetUserProfile", 'a' * 8001, "opaque", 100, 50)
        assert not oversize.success
        assert json.loads(oversize.content)["error"]["code"] == "INVALID_ARGUMENTS"
    assert not calls


async def test_callback_missing_usage_and_redirects_are_not_accepted():
    for response in (httpx.Response(200, json={"content": "{}", "success": True, "sources": []}),
                     httpx.Response(307, headers={"location": "https://evil.example"})):
        async with httpx.AsyncClient(transport=httpx.MockTransport(lambda _: response), follow_redirects=False) as client:
            with pytest.raises(ServiceError):
                await DotnetTools(settings(), client).execute("GetUserProfile", "{}", "opaque", 100, 50)


@pytest.mark.parametrize("phase", ["provider", "callback"])
async def test_real_http_operations_propagate_cancellation_without_retry(phase):
    started = asyncio.Event()
    cancelled = asyncio.Event()
    calls = 0

    async def handler(request):
        nonlocal calls
        calls += 1
        started.set()
        try:
            await asyncio.Future()
        finally:
            cancelled.set()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler), follow_redirects=False) as http:
        async with AsyncOpenAI(api_key="test-only-key", base_url="https://openrouter.ai/api/v1", max_retries=0, http_client=http) as sdk:
            if phase == "provider":
                work = OpenRouterProvider(settings(), sdk).complete([{"role": "user", "content": "beasiswa"}], 123)
            else:
                work = DotnetTools(settings(), http).execute("GetRecommendations", '{"essay":"facts"}', "opaque", 100, 50)
            task = asyncio.create_task(work)
            await asyncio.wait_for(started.wait(), 1)
            task.cancel()
            with pytest.raises(asyncio.CancelledError):
                await task
    assert calls == 1 and cancelled.is_set()
