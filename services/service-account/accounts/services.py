"""Business logic for account operations.

This layer sits between views (HTTP) and models (database).
It orchestrates data mutations and triggers RabbitMQ publishing.

With the immutable-data pattern every "update" creates a new
AccountDetail row instead of mutating the previous one, preserving
full history.  "Delete" inserts a DeletedAccount record rather
than removing any rows.
"""

import logging

from accounts import publishers
from accounts.models import Account, AccountDetail, AccountType, DeletedAccount
from django.db import IntegrityError, transaction  # type: ignore[import-untyped]

logger = logging.getLogger(__name__)


class DuplicateAccountError(Exception):
    """Raised when an owner already has a live account with the same name and type."""


# Queries


def get_account_by_guid(guid):
    """Return an account by its GUID; raise DoesNotExist if deleted or missing."""
    account = Account.objects.get(account_guid=guid)
    if account.deleted_records.exists():
        raise Account.DoesNotExist("Account has been deleted.")
    return account


# Contract 1 – Create Account


def _has_duplicate_account(owner_id: str, name: str, account_type_name: str) -> bool:
    """Return True if the owner already has a live account with this name and type.

    Checks the *latest* AccountDetail per account so renamed accounts are not
    counted against the original name.
    """
    deleted_ids = DeletedAccount.objects.values_list("account_id", flat=True)
    active_accounts = (
        Account.objects.select_for_update()
        .filter(owner_id=owner_id)
        .exclude(id__in=deleted_ids)
    )
    for account in active_accounts:
        latest = account.details.order_by("-timestamp").first()
        if (
            latest
            and latest.name == name
            and latest.account_type.name == account_type_name
        ):
            return True
    return False


def create_account(
    owner_id: str,
    name: str,
    account_type_name: str,
    account_guid: str | None = None,
) -> tuple[Account, bool]:
    """Create Account + initial AccountDetail, then publish to RabbitMQ.

    Returns a tuple of (account, created) where *created* is False when
    the account already existed (idempotent retry with the same GUID).
    """
    # Idempotency: if a GUID was supplied and already exists, return it.
    if account_guid is not None:
        try:
            existing = Account.objects.get(account_guid=account_guid)
            if not existing.deleted_records.exists():
                return existing, False
        except Account.DoesNotExist:
            pass

    with transaction.atomic():
        if _has_duplicate_account(owner_id, name, account_type_name):
            raise DuplicateAccountError(
                f"Account '{name}' of type '{account_type_name}' already exists for this owner."
            )

        account_type = AccountType.objects.get(name=account_type_name)

        create_kwargs: dict = {"owner_id": owner_id}
        if account_guid is not None:
            create_kwargs["account_guid"] = account_guid

        try:
            account = Account.objects.create(**create_kwargs)
        except IntegrityError:
            # Race condition: another request with the same GUID won.
            account = Account.objects.get(account_guid=account_guid)
            return account, False

        detail = AccountDetail.objects.create(
            account=account,
            name=name,
            account_type=account_type,
        )

    publishers.publish_account_created(
        {
            "account_guid": account.account_guid,
            "owner_id": owner_id,
            "type": account_type_name,
            "name": name,
            "is_frozen": detail.is_frozen,
            "timestamp": detail.timestamp.isoformat(),
        }
    )

    return account, True


# Contract 2 – Freeze / Unfreeze


def freeze_account(account: Account, freeze: bool) -> Account:
    """Create a new AccountDetail toggling the frozen flag."""
    latest = account.details.order_by("-timestamp").first()
    if not latest:
        raise ValueError("Account has no details.")

    detail = AccountDetail.objects.create(
        account=account,
        name=latest.name,
        account_type=latest.account_type,
        is_frozen=freeze,
    )

    publishers.publish_account_frozen(
        account.owner_id,
        account.account_guid,
        freeze,
        detail.timestamp.isoformat(),
    )
    return account


# Contract 3 – Update Account (rename / change type)


def update_account(
    account: Account,
    name: str,
    account_type_name: str,
) -> Account:
    """Create a new AccountDetail with the updated name / type."""
    latest = account.details.order_by("-timestamp").first()
    account_type = AccountType.objects.get(name=account_type_name)

    detail = AccountDetail.objects.create(
        account=account,
        name=name,
        account_type=account_type,
        is_frozen=latest.is_frozen if latest else False,
    )

    publishers.publish_account_updated(
        account.owner_id,
        account.account_guid,
        name,
        account_type_name,
        detail.timestamp.isoformat(),
    )
    return account


# Contract 4 – Soft-delete Account


def soft_delete_account(account: Account) -> DeletedAccount:
    """Mark an account as deleted (immutable – no rows are removed)."""
    deleted = DeletedAccount.objects.create(account=account)

    publishers.publish_account_deleted(
        account.account_guid,
        account.owner_id,
        deleted.timestamp.isoformat(),
    )
    return deleted


# Contract 5 – Initiate Bank Transaction


def initiate_transaction(
    account: Account,
    payload: dict,
) -> None:
    """Look up account details, resolve receiver, publish to Fraud-Service."""
    owner_id = payload["owner_id"]
    receiver_guid = payload.get("receiver_guid")
    amount = payload["amount"]
    transaction_type = payload["transaction_type"]
    message_id = payload["message_id"]
    origin_ip = payload["origin_ip"]

    latest = account.details.order_by("-timestamp").first()
    account_data = {
        "guid": account.account_guid,
        "name": latest.name if latest else "",
        "type": latest.account_type.name if latest else "",
    }

    receiver_data = None
    if receiver_guid and transaction_type == "TRANSFER":
        receiver = Account.objects.get(account_guid=receiver_guid)
        rl = receiver.details.order_by("-timestamp").first()
        receiver_data = {
            "guid": receiver.account_guid,
            "name": rl.name if rl else "",
            "type": rl.account_type.name if rl else "",
        }

    publishers.publish_transaction(
        {
            "owner_id": owner_id,
            "account_data": account_data,
            "receiver_data": receiver_data,
            "amount": amount,
            "transaction_type": transaction_type,
            "message_id": message_id,
            "origin_ip": origin_ip,
        }
    )


# Contract 6 - Initiate Currency Exchange


def initiate_exchange(
    account: Account,
    payload: dict,
) -> None:
    """Publish exchange request to Fraud-Service (Contract 6 Step 2)."""
    owner_id = payload["owner_id"]
    amount = payload["amount"]
    currency = payload["currency"]
    message_id = payload["message_id"]
    origin_ip = payload["origin_ip"]

    latest = account.details.order_by("-timestamp").first()
    account_data = {
        "guid": account.account_guid,
        "name": latest.name if latest else "",
        "type": latest.account_type.name if latest else "",
    }

    publishers.publish_exchange_request(
        {
            "owner_id": owner_id,
            "account_data": account_data,
            "amount": amount,
            "currency": currency,
            "message_id": message_id,
            "origin_ip": origin_ip,
        }
    )


# Saga – Compensating transaction for failed balance creation


def compensate_account_creation(account_guid: str, reason: str) -> None:
    """Rollback account creation when balance creation fails in Transaction-Service."""
    try:
        account = Account.objects.get(account_guid=account_guid)
    except Account.DoesNotExist:
        logger.warning(
            "Compensating transaction skipped – account %s not found"
            " (already deleted?).",
            account_guid,
        )
        return

    if account.deleted_records.exists():
        logger.info(
            "Compensating transaction skipped – account %s already deleted.",
            account_guid,
        )
        return

    soft_delete_account(account)
    logger.info(
        "Compensating transaction completed – account %s deleted. Reason: %s",
        account_guid,
        reason,
    )
