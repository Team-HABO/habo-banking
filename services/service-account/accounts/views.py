"""HTTP endpoint handlers for the account service.

Views are intentionally thin – they parse the request, delegate to
the *services* layer for business logic, and return a JSON response.
"""

import json
import logging

from accounts import services
from accounts.models import Account
from accounts.serializers import (
    AccountResponseSerializer,
    CreateAccountSerializer,
    ExchangeSerializer,
    FreezeAccountSerializer,
    TransactionSerializer,
    UpdateAccountSerializer,
)
from django.http import JsonResponse  # type: ignore[import-untyped]
from django.views.decorators.csrf import csrf_exempt  # type: ignore[import-untyped]
from django.views.decorators.http import (  # type: ignore[import-untyped]
    require_http_methods,
)

logger = logging.getLogger(__name__)


# Utility


def _parse_json(request):
    """Return parsed JSON body or None."""
    try:
        return json.loads(request.body)
    except (json.JSONDecodeError, ValueError):
        return None


def _get_client_ip(request):
    """Extract the client IP from the request."""
    forwarded = request.META.get("HTTP_X_FORWARDED_FOR")
    if forwarded:
        return forwarded.split(",")[0].strip()
    return request.META.get("REMOTE_ADDR", "")


# /accounts  (POST create)


@csrf_exempt
@require_http_methods(["POST"])
def account_list(request):
    """Create a new account (POST)."""
    return _create_account(request)


def _create_account(request):
    body = _parse_json(request)
    if body is None:
        return JsonResponse({"error": "Invalid JSON body."}, status=400)

    # owner_id currently comes from request body until JWT integration is added.
    owner_id = body.get("owner_id")
    if not owner_id:
        return JsonResponse({"error": "owner_id is required."}, status=400)

    serializer = CreateAccountSerializer(data=body)
    if not serializer.is_valid():
        return JsonResponse({"errors": serializer.errors}, status=400)

    account = services.create_account(
        owner_id=owner_id,
        name=serializer.validated_data["name"],
        account_type_name=serializer.validated_data["type"],
    )

    return JsonResponse(AccountResponseSerializer(account).data, status=201)


# /accounts/<guid>  (GET, PUT, PATCH, DELETE)


@csrf_exempt
@require_http_methods(["PUT", "PATCH", "DELETE"])
def account_detail(request, guid):
    """Single-account operations dispatched by HTTP method."""
    try:
        account = services.get_account_by_guid(guid)
    except Account.DoesNotExist:
        return JsonResponse({"error": "Account not found."}, status=404)

    if request.method == "PUT":
        return _update_account(request, account)
    if request.method == "PATCH":
        return _freeze_account(request, account)
    return _delete_account(account)


def _update_account(request, account):
    body = _parse_json(request)
    if body is None:
        return JsonResponse({"error": "Invalid JSON body."}, status=400)

    serializer = UpdateAccountSerializer(data=body)
    if not serializer.is_valid():
        return JsonResponse({"errors": serializer.errors}, status=400)

    account = services.update_account(
        account=account,
        name=serializer.validated_data["name"],
        account_type_name=serializer.validated_data["type"],
    )

    return JsonResponse(AccountResponseSerializer(account).data)


def _freeze_account(request, account):
    body = _parse_json(request)
    if body is None:
        return JsonResponse({"error": "Invalid JSON body."}, status=400)

    serializer = FreezeAccountSerializer(data=body)
    if not serializer.is_valid():
        return JsonResponse({"errors": serializer.errors}, status=400)

    account = services.freeze_account(
        account=account,
        freeze=serializer.validated_data["freeze"],
    )

    return JsonResponse(AccountResponseSerializer(account).data)


def _delete_account(account):
    services.soft_delete_account(account)
    return JsonResponse({"message": "Account deleted."}, status=200)


# /accounts/<guid>/transactions  (POST)


@csrf_exempt
@require_http_methods(["POST"])
def account_transactions(request, guid):
    """Initiate a bank transaction (Contract 5)."""
    try:
        account = services.get_account_by_guid(guid)
    except Account.DoesNotExist:
        return JsonResponse({"error": "Account not found."}, status=404)

    body = _parse_json(request)
    if body is None:
        return JsonResponse({"error": "Invalid JSON body."}, status=400)

    serializer = TransactionSerializer(data=body)
    if not serializer.is_valid():
        return JsonResponse({"errors": serializer.errors}, status=400)

    vd = serializer.validated_data
    services.initiate_transaction(
        account=account,
        payload={
            "owner_id": account.owner_id,
            "receiver_guid": vd.get("receiverAccountGuid"),
            "amount": vd["amount"],
            "transaction_type": vd["transactionType"],
            "message_id": str(vd["messageId"]),
            "origin_ip": _get_client_ip(request),
        },
    )

    return JsonResponse({"message": "Transaction initiated."}, status=202)


# /accounts/<guid>/exchanges  (POST)


@csrf_exempt
@require_http_methods(["POST"])
def account_exchanges(request, guid):
    """Initiate a currency exchange request (Contract 6)."""
    try:
        account = services.get_account_by_guid(guid)
    except Account.DoesNotExist:
        return JsonResponse({"error": "Account not found."}, status=404)

    body = _parse_json(request)
    if body is None:
        return JsonResponse({"error": "Invalid JSON body."}, status=400)

    serializer = ExchangeSerializer(data=body)
    if not serializer.is_valid():
        return JsonResponse({"errors": serializer.errors}, status=400)

    vd = serializer.validated_data
    services.initiate_exchange(
        account=account,
        payload={
            "owner_id": account.owner_id,
            "amount": vd["amount"],
            "currency": vd["currency"],
            "message_id": str(vd["messageId"]),
            "origin_ip": _get_client_ip(request),
        },
    )

    return JsonResponse({"message": "Exchange initiated."}, status=202)
