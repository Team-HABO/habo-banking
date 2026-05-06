"""Django management command to start the RabbitMQ consumer.

Usage:
    python manage.py consume_events
"""

import logging

from accounts.consumers import start_consuming
from django.core.management.base import BaseCommand  # type: ignore[import-untyped]

logger = logging.getLogger(__name__)


class Command(BaseCommand):
    """Start the RabbitMQ consumer for compensating events."""

    help = "Start consuming RabbitMQ events (saga compensating transactions)."

    def handle(self, *args, **options):
        self.stdout.write("Starting RabbitMQ consumer for compensating events...")
        try:
            start_consuming()
        except KeyboardInterrupt:
            self.stdout.write("Consumer stopped.")
