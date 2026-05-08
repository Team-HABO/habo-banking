"""RabbitMQ test consumer helper.

Binds a temporary exclusive queue to an exchange BEFORE a test runs,
then lets you read the message that arrived AFTER the action.

Usage:
    consumer = RabbitMQTestConsumer("account-exchange-events", "fanout")
    # ... trigger HTTP request ...
    message = consumer.get_message()
    consumer.close()
"""

import json
import os
import time

import pika  # type: ignore[import-untyped]

RABBITMQ_HOST = os.getenv("RABBITMQ_HOST", "localhost")
RABBITMQ_PORT = int(os.getenv("RABBITMQ_PORT", "5672"))
RABBITMQ_USER = os.getenv("RABBITMQ_USER", "guest")
RABBITMQ_PASSWORD = os.getenv("RABBITMQ_PASSWORD", "guest")


class RabbitMQTestConsumer:
    """Temporary queue bound to an exchange for verifying published messages in tests."""

    def __init__(self, exchange: str, exchange_type: str = "fanout", routing_key: str = ""):
        credentials = pika.PlainCredentials(RABBITMQ_USER, RABBITMQ_PASSWORD)
        self.connection = pika.BlockingConnection(
            pika.ConnectionParameters(
                host=RABBITMQ_HOST,
                port=RABBITMQ_PORT,
                credentials=credentials,
            )
        )
        self.channel = self.connection.channel()
        # Declare the same exchange the publisher uses (idempotent, safe to call multiple times)
        self.channel.exchange_declare(
            exchange=exchange,
            exchange_type=exchange_type,
            durable=True,
        )
        # Exclusive queue — auto-deleted when connection closes
        result = self.channel.queue_declare(queue="", exclusive=True)
        self.queue_name = result.method.queue
        self.channel.queue_bind(
            exchange=exchange,
            queue=self.queue_name,
            routing_key=routing_key,
        )

    def get_message(self, timeout: float = 3.0) -> dict | None:
        """Poll the queue until a message arrives or timeout is reached."""
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            method, _props, body = self.channel.basic_get(
                queue=self.queue_name, auto_ack=True
            )
            if body is not None:
                return json.loads(body)
            time.sleep(0.1)
        return None

    def close(self) -> None:
        self.connection.close()
