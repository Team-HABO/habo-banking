"""Account app configuration."""

from django.apps import AppConfig  # type: ignore[import-untyped]


class AccountsConfig(AppConfig):
    """Configuration for the accounts app."""

    default_auto_field = "django.db.models.BigAutoField"
    name = "accounts"
