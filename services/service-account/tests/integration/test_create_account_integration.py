"""Integration tests for POST /v1/accounts/ – create account."""

import json

from accounts.models import Account, AccountDetail, AccountType
from django.test import Client, TransactionTestCase # type: ignore[import-untyped]
from tests.integration.helpers import RabbitMQTestConsumer


class CreateAccountIntegrationTests(TransactionTestCase):
    """Integration tests for create account – real Postgres + real RabbitMQ."""

    def setUp(self):
        self.client = Client()
        self.url = "/v1/accounts/"
        # TransactionTestCase flushes all tables between tests, so we must
        # recreate the seeded account types here.
        for name in ("checking", "savings", "pension"):
            AccountType.objects.get_or_create(name=name)

        self.valid_body = {
            "owner_id": "owner-integration-123",
            "name": "My Savings",
            "type": "savings",
        }

    # Database side effects
    def test_creates_account_and_detail_row_in_postgres(self):
        response = self.client.post(
            self.url,
            data=json.dumps(self.valid_body),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 201)

        account = Account.objects.get(owner_id="owner-integration-123")
        detail = AccountDetail.objects.filter(account=account).first()

        self.assertIsNotNone(account)
        self.assertIsNotNone(detail)
        self.assertEqual(detail.name, "My Savings")
        self.assertEqual(detail.account_type.name, "savings")
        self.assertFalse(detail.is_frozen)

    def test_response_contains_account_guid(self):
        response = self.client.post(
            self.url,
            data=json.dumps(self.valid_body),
            content_type="application/json",
        )

        data = json.loads(response.content)
        self.assertIn("account_guid", data)

    # RabbitMQ side effects – message actually published to the exchange

    def test_publishes_to_account_exchange_events(self):
        # Bind a temp queue to the exchange BEFORE the request so we catch the message
        consumer = RabbitMQTestConsumer("account-exchange-events", "fanout")
        try:
            self.client.post(
                self.url,
                data=json.dumps(self.valid_body),
                content_type="application/json",
            )

            message = consumer.get_message()
            self.assertIsNotNone(message, "No message arrived on account-exchange-events")
            self.assertEqual(message["data"]["ownerId"], "owner-integration-123")
            self.assertEqual(message["data"]["name"], "My Savings")
            self.assertEqual(message["data"]["type"], "savings")
            self.assertIn("accountGuid", message["data"])
            self.assertEqual(message["metadata"]["messageType"], "ACCOUNT_CREATE")
        finally:
            consumer.close()

    def test_publishes_to_synchronize_events(self):
        consumer = RabbitMQTestConsumer(
            "synchronize-events",
            exchange_type="direct",
            routing_key="synchronize-account-queue",
        )
        try:
            self.client.post(
                self.url,
                data=json.dumps(self.valid_body),
                content_type="application/json",
            )

            message = consumer.get_message()
            self.assertIsNotNone(message, "No message arrived on synchronize-events")
            self.assertEqual(message["metadata"]["messageType"], "ACCOUNT_CREATE")
        finally:
            consumer.close()

    # Duplicate check – enforced at the database level

    def test_duplicate_returns_409_and_only_one_row_in_postgres(self):
        self.client.post(
            self.url,
            data=json.dumps(self.valid_body),
            content_type="application/json",
        )

        response = self.client.post(
            self.url,
            data=json.dumps(self.valid_body),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 409)
        self.assertEqual(
            Account.objects.filter(owner_id="owner-integration-123").count(), 1
        )

    def test_duplicate_publishes_only_once_to_rabbitmq(self):
        consumer = RabbitMQTestConsumer("account-exchange-events", "fanout")
        try:
            for _ in range(3):
                self.client.post(
                    self.url,
                    data=json.dumps(self.valid_body),
                    content_type="application/json",
                )

            # First message should arrive
            first = consumer.get_message(timeout=3.0)
            self.assertIsNotNone(first)

            # No second message should arrive
            second = consumer.get_message(timeout=1.0)
            self.assertIsNone(second, "Duplicate request incorrectly published a second message")
        finally:
            consumer.close()

    # Different owners / names are not blocked

    def test_different_owners_can_have_same_name_and_type(self):
        body_a = {**self.valid_body, "owner_id": "owner-aaa"}
        body_b = {**self.valid_body, "owner_id": "owner-bbb"}

        r1 = self.client.post(self.url, data=json.dumps(body_a), content_type="application/json")
        r2 = self.client.post(self.url, data=json.dumps(body_b), content_type="application/json")

        self.assertEqual(r1.status_code, 201)
        self.assertEqual(r2.status_code, 201)
        self.assertEqual(Account.objects.count(), 2)
