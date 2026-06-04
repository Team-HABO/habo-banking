"""URL configuration for account_service project.

Root-level routing that delegates /accounts/ to the accounts app.
"""

from django.urls import include, path  # type: ignore[import-untyped]
from drf_spectacular.views import SpectacularAPIView, SpectacularSwaggerView  # type: ignore[import-untyped]

urlpatterns = [
    path("v1/accounts/", include("accounts.urls")),
    # OpenAPI schema (raw YAML/JSON)
    path("api/schema/", SpectacularAPIView.as_view(), name="schema"),
    # Swagger UI
    path("api/docs/", SpectacularSwaggerView.as_view(url_name="schema"), name="swagger-ui"),
]
