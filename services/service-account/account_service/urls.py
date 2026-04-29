"""URL configuration for account_service project.

Root-level routing that delegates /accounts/ to the accounts app.
"""

from django.urls import include, path  # type: ignore[import-untyped]

urlpatterns = [
    path("accounts/", include("accounts.urls")),
]
