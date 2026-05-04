"""RabbitMQ message consumers for the account service.

Listens for compensating events from other services to maintain
data consistency (saga pattern).
"""

import json
import logging
import os

import pika  # type: ignore[import-untyped]
from django.db import DatabaseError

from accounts import services

logger = logging.getLogger(__name__)

RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
RABBITMQ_PORT = int(os.getenv("RABBITMQ_PORT", "5672"))
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASSWORD = os.getenv("RABBITMQ_PASSWORD", "")

EXCHANGE_ACCOUNT_CREATE_RESPONSE = "account-create-response"
QUEUE_ACCOUNT_CREATE_FAILED = "account-create-failed-queue"


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


def _on_balance_create_failed(ch, method, _properties, body):
    """Handle BALANCE_CREATE_FAILED – compensating transaction."""
    try:
        message = json.loads(body)
        data = message.get("data", {})
        account_guid = data.get("accountGuid")
        reason = data.get("reason", "unknown")

        if not account_guid:
            logger.error("BALANCE_CREATE_FAILED missing accountGuid: %s", message)
            ch.basic_nack(delivery_tag=method.delivery_tag, requeue=False)
            return

        logger.info(
            "Received BALANCE_CREATE_FAILED for account %s. Reason: %s",
            account_guid,
            reason,
        )

        services.compensate_account_creation(account_guid, reason)
        ch.basic_ack(delivery_tag=method.delivery_tag)

    except json.JSONDecodeError:
        logger.error("Invalid JSON in BALANCE_CREATE_FAILED message.")
        ch.basic_nack(delivery_tag=method.delivery_tag, requeue=False)
    except (DatabaseError, pika.exceptions.AMQPError):
        logger.exception("Error processing BALANCE_CREATE_FAILED.")
        ch.basic_nack(delivery_tag=method.delivery_tag, requeue=True)


def start_consuming():
    """Connect to RabbitMQ and consume compensating events.

    This is a blocking call – designed to be run from a management command.
    """
    connection = _get_connection()
    channel = connection.channel()

    channel.exchange_declare(
        exchange=EXCHANGE_ACCOUNT_CREATE_RESPONSE,
        exchange_type="direct",
        durable=True,
    )
    channel.queue_declare(queue=QUEUE_ACCOUNT_CREATE_FAILED, durable=True)
    channel.queue_bind(
        queue=QUEUE_ACCOUNT_CREATE_FAILED,
        exchange=EXCHANGE_ACCOUNT_CREATE_RESPONSE,
        routing_key=QUEUE_ACCOUNT_CREATE_FAILED,
    )

    channel.basic_qos(prefetch_count=1)
    channel.basic_consume(
        queue=QUEUE_ACCOUNT_CREATE_FAILED,
        on_message_callback=_on_balance_create_failed,
    )

    logger.info(
        "Listening for compensating events on queue '%s'...",
        QUEUE_ACCOUNT_CREATE_FAILED,
    )
    channel.start_consuming()
