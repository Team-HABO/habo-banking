"""URL configuration for account_service project."""

from accounts import views
from django.urls import path  # type: ignore[import-untyped]

urlpatterns = [
    path("", views.root, name="root"),
    path("health/", views.health, name="health"),
]
