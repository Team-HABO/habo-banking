"""RabbitMQ message publishers for the account service.

Each public function maps to a contract step that produces messages
to other microservices (Transaction-Service, Synchronize-Service,
Fraud-Service, Currency-Service).

Connection parameters are read from environment variables so they
can be overridden per deployment environment.
"""

import json
import logging
import os
from datetime import datetime, timezone

import pika  # type: ignore[import-untyped]

logger = logging.getLogger(__name__)

RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
RABBITMQ_PORT = int(os.getenv("RABBITMQ_PORT", "5672"))
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASSWORD = os.getenv("RABBITMQ_PASSWORD", "guest")
EXCHANGE_NAME = os.getenv("RABBITMQ_EXCHANGE", "habo_exchange")

# Internal helpers

def _get_connection():
    """Create a new blocking connection to RabbitMQ."""
    credentials = pika.PlainCredentials(RABBITMQ_USER, RABBITMQ_PASSWORD)
    return pika.BlockingConnection(
        pika.ConnectionParameters(
            host=RABBITMQ_HOST,
            port=RABBITMQ_PORT,
            credentials=credentials,
        )
    )


def _publish(routing_key: str, message: dict) -> None:
    """Publish a JSON message to the topic exchange."""
    connection = _get_connection()
    try:
        channel = connection.channel()
        channel.exchange_declare(
            exchange=EXCHANGE_NAME,
            exchange_type="topic",
            durable=True,
        )
        channel.basic_publish(
            exchange=EXCHANGE_NAME,
            routing_key=routing_key,
            body=json.dumps(message),
            properties=pika.BasicProperties(
                delivery_mode=2,
                content_type="application/json",
            ),
        )
        logger.info(
            "Published %s → %s",
            message["metadata"]["messageType"],
            routing_key,
        )
    finally:
        connection.close()


def _build_metadata(message_type: str, message_id: str | None = None) -> dict:
    """Build the standard metadata envelope."""
    meta = {
        "messageType": message_type,
        "messageTimestamp": datetime.now(timezone.utc).isoformat(),
    }
    if message_id:
        meta["messageId"] = message_id
    return meta


# Contract 1 – Account Created

def publish_account_created(account_data: dict) -> None:
    """Publish ACCOUNT_CREATE to Transaction-Service and Synchronize-Service."""
    # Step 2 → Transaction-Service
    _publish("account.created.transaction", {
        "data": {
            "accountGuid": str(account_data["account_guid"]),
            "ownerId": account_data["owner_id"],
            "type": account_data["type"],
            "name": account_data["name"],
            "isFrozen": account_data["is_frozen"],
            "timestamp": account_data["timestamp"],
        },
        "metadata": _build_metadata("ACCOUNT_CREATE"),
    })

    # Step 3 → Synchronize-Service
    _publish("account.created.sync", {
        "data": {
            "account": {
                "accountGuid": str(account_data["account_guid"]),
                "type": account_data["type"],
                "name": account_data["name"],
                "isFrozen": account_data["is_frozen"],
                "timestamp": account_data["timestamp"],
                "balance": {
                    "amount": "0",
                    "timestamp": account_data["timestamp"],
                },
            },
        },
        "metadata": _build_metadata("ACCOUNT_CREATE"),
    })

# Contract 2 – Account Frozen / Unfrozen

def publish_account_frozen(account_guid, is_frozen: bool, timestamp: str) -> None:
    """Publish ACCOUNT_STATUS to Synchronize-Service."""
    _publish("account.status.sync", {
        "data": {
            "account": {
                "accountGuid": str(account_guid),
                "isFrozen": is_frozen,
                "timestamp": timestamp,
            },
        },
        "metadata": _build_metadata("ACCOUNT_STATUS"),
    })


# Contract 3 – Account Updated (rename / type change)

def publish_account_updated(
    account_guid, name: str, account_type: str, timestamp: str,
) -> None:
    """Publish ACCOUNT_UPDATE to Synchronize-Service."""
    _publish("account.updated.sync", {
        "data": {
            "account": {
                "accountGuid": str(account_guid),
                "name": name,
                "type": account_type,
                "timestamp": timestamp,
            },
        },
        "metadata": _build_metadata("ACCOUNT_UPDATE"),
    })


# Contract 4 – Account Deleted (soft)

def publish_account_deleted(
    account_guid, owner_id: str, timestamp: str,
) -> None:
    """Publish ACCOUNT_DELETE to Transaction-Service and Synchronize-Service."""
    # Step 2 → Transaction-Service
    _publish("account.deleted.transaction", {
        "data": {
            "accountGuid": str(account_guid),
            "ownerId": owner_id,
            "timestamp": timestamp,
        },
        "metadata": _build_metadata("ACCOUNT_DELETE"),
    })

    # Step 3 → Synchronize-Service
    _publish("account.deleted.sync", {
        "data": {
            "account": {
                "accountGuid": str(account_guid),
                "timestamp": timestamp,
            },
        },
        "metadata": _build_metadata("ACCOUNT_DELETE"),
    })

# Contract 5 – Bank Transaction

def publish_transaction(
    account_data: dict,
    receiver_data: dict | None,
    amount: str,
    transaction_type: str,
    message_id: str,
    origin_ip: str,
) -> None:
    """Publish fraud-check request to Fraud-Service (Contract 5, Step 2)."""
    data: dict = {
        "account": {
            "guid": str(account_data["guid"]),
            "name": account_data["name"],
            "type": account_data["type"],
        },
        "amount": amount,
        "transactionType": transaction_type,
        "originIpAddress": origin_ip,
    }
    if receiver_data:
        data["receiver"] = {
            "guid": str(receiver_data["guid"]),
            "name": receiver_data["name"],
            "type": receiver_data["type"],
        }

    _publish("transaction.fraud.check", {
        "data": data,
        "metadata": _build_metadata(
            f"TRANSACTION_{transaction_type}", message_id,
        ),
    })



