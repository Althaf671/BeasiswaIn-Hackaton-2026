import pytest

from app.settings import Settings


@pytest.fixture(autouse=True)
def isolated_service_settings(monkeypatch):
    for field in Settings.model_fields.values():
        monkeypatch.delenv(field.validation_alias, raising=False)
    # Boundary scenarios use explicit fixture budgets, not deployment defaults.
    for name, value in {
        "SCHOLARBOT_MAX_ITERATIONS": "6",
        "SCHOLARBOT_MAX_TOOL_CALLS": "8",
        "SCHOLARBOT_MAX_OUTPUT_TOKENS": "6000",
        "SCHOLARBOT_MAX_TOTAL_TOKENS": "48000",
    }.items():
        monkeypatch.setenv(name, value)
