"""Account service views."""

from django.http import JsonResponse  # type: ignore[import-untyped]


def health(request):
    """Return service health status."""
    return JsonResponse({"status": "ok"})


def root(request):
    """Return welcome message."""
    return JsonResponse({"message": "Welcome to the Account Service microservice"})
