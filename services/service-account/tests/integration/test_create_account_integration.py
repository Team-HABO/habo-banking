"""Integration tests for POST /v1/accounts/ – create account."""

import json
import os

import jwt  # type: ignore[import-untyped]
from accounts.models import Account, AccountDetail, AccountType
from django.test import Client, TransactionTestCase  # type: ignore[import-untyped]
from tests.integration.helpers import RabbitMQTestConsumer

TEST_JWT_SECRET = "test-secret-key-for-integration-tests-xxxxx"


class CreateAccountIntegrationTests(TransactionTestCase):
    """Integration tests for create account – real Postgres + real RabbitMQ."""

    def setUp(self):
        os.environ["JWT_SECRET_KEY"] = TEST_JWT_SECRET
        self.client = Client()
        self.url = "/v1/accounts/"
        # TransactionTestCase flushes all tables between tests, so we must
        # recreate the seeded account types here.
        for name in ("checking", "savings", "pension"):
            AccountType.objects.get_or_create(name=name)

        self.owner_id = "owner-integration-123"
        self.token = self._make_token(self.owner_id)
        self.valid_body = {
            "name": "My Savings",
            "type": "savings",
        }

    def _make_token(self, owner_id: str) -> str:
        """Return a signed HS256 JWT with the given owner_id as the `nameid` claim."""
        return jwt.encode({"nameid": owner_id}, TEST_JWT_SECRET, algorithm="HS256")

    def _post(self, body: dict, token: str | None = None):
        """POST to /v1/accounts/ with the given JWT (defaults to self.token)."""
        auth = token or self.token
        return self.client.post(
            self.url,
            data=json.dumps(body),
            content_type="application/json",
            HTTP_AUTHORIZATION=f"Bearer {auth}",
        )

    # Database side effects
    def test_creates_account_and_detail_row_in_postgres(self):
        response = self._post(self.valid_body)

        self.assertEqual(response.status_code, 201)

        account = Account.objects.get(owner_id=self.owner_id)
        detail = AccountDetail.objects.filter(account=account).first()

        self.assertIsNotNone(account)
        self.assertIsNotNone(detail)
        self.assertEqual(detail.name, "My Savings")
        self.assertEqual(detail.account_type.name, "savings")
        self.assertFalse(detail.is_frozen)

    def test_response_contains_account_guid(self):
        response = self._post(self.valid_body)

        data = json.loads(response.content)
        self.assertIn("account_guid", data)

    # RabbitMQ side effects – message actually published to the exchange

    def test_publishes_to_account_exchange_events(self):
        # Bind a temp queue to the exchange BEFORE the request so we catch the message
        consumer = RabbitMQTestConsumer("account-exchange-events", "fanout")
        try:
            self._post(self.valid_body)

            message = consumer.get_message()
            self.assertIsNotNone(
                message, "No message arrived on account-exchange-events"
            )
            self.assertEqual(message["data"]["ownerId"], self.owner_id)
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
            self._post(self.valid_body)

            message = consumer.get_message()
            self.assertIsNotNone(message, "No message arrived on synchronize-events")
            self.assertEqual(message["metadata"]["messageType"], "ACCOUNT_CREATE")
        finally:
            consumer.close()

    # Duplicate check – enforced at the database level

    def test_duplicate_returns_409_and_only_one_row_in_postgres(self):
        self._post(self.valid_body)
        response = self._post(self.valid_body)

        self.assertEqual(response.status_code, 409)
        self.assertEqual(Account.objects.filter(owner_id=self.owner_id).count(), 1)

    def test_duplicate_publishes_only_once_to_rabbitmq(self):
        consumer = RabbitMQTestConsumer("account-exchange-events", "fanout")
        try:
            for _ in range(3):
                self._post(self.valid_body)

            # First message should arrive
            first = consumer.get_message(timeout=3.0)
            self.assertIsNotNone(first)

            # No second message should arrive
            second = consumer.get_message(timeout=1.0)
            self.assertIsNone(
                second, "Duplicate request incorrectly published a second message"
            )
        finally:
            consumer.close()

    # Different owners / names are not blocked

    def test_different_owners_can_have_same_name_and_type(self):
        r1 = self._post(self.valid_body, token=self._make_token("owner-aaa"))
        r2 = self._post(self.valid_body, token=self._make_token("owner-bbb"))

        self.assertEqual(r1.status_code, 201)
        self.assertEqual(r2.status_code, 201)
        self.assertEqual(Account.objects.count(), 2)
