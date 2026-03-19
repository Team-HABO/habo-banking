"""URL configuration for account_service project."""

from django.urls import path  # type: ignore[import-untyped]

from accounts import views

urlpatterns = [
    path("", views.root, name="root"),
    path("health/", views.health, name="health"),
]
