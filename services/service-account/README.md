# Service Account

Django microservice responsible for account commands (create, update, freeze, delete, transaction start, exchange start).

This service uses:
- PostgreSQL for persistence
- RabbitMQ for publishing events to other services

## Architecture

- `accounts/views.py`: HTTP handlers
- `accounts/serializers.py`: request validation + response shaping
- `accounts/services.py`: business logic
- `accounts/publishers.py`: RabbitMQ publishing
- `accounts/models.py`: ORM models

Root routing is defined in `account_service/urls.py` and mounts all endpoints under `/accounts/`.

## Endpoints

All endpoints are rooted at `/accounts/`.

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/accounts/` | Create account |
| PUT | `/accounts/{guid}/` | Rename / change account type |
| PATCH | `/accounts/{guid}/` | Freeze/unfreeze |
| DELETE | `/accounts/{guid}/` | Soft delete |
| POST | `/accounts/{guid}/transactions/` | Initiate transfer/withdraw/deposit |
| POST | `/accounts/{guid}/exchanges/` | Initiate currency exchange |

## Example request bodies

Create account:

```json
{
   "owner_id": "user-123",
   "name": "My Savings",
   "type": "savings"
}
```

Freeze/unfreeze:

```json
{
   "freeze": true
}
```

Rename/change type:

```json
{
   "name": "Daily Account",
   "type": "checking"
}
```

Transaction:

```json
{
   "amount": "500",
   "transactionType": "DEPOSIT",
   "messageId": "550e8400-e29b-41d4-a716-446655440000"
}
```

Exchange:

```json
{
   "amount": "100",
   "currency": "EUR",
   "messageId": "40ff6f1a-3b84-43a6-b440-d15918f5bc64"
}
```

## RabbitMQ publishing

The service publishes events to these exchanges:

- `account-exchange-events` (fanout): account create/delete events
- `synchronize-events` (direct): account state sync events
- `ai-service-transaction` (fanout): fraud-check requests for transactions/exchanges

Default routing key used for synchronize events:

- `synchronize-account-queue`

## Run locally (dev container)

1. Reopen the folder in dev container
2. Run migrations
3. Start server

```bash
python manage.py migrate
python manage.py runserver 0.0.0.0:8000
```

RabbitMQ management UI is available at `http://localhost:15672`.

## Database notes

The service uses immutable account history:

- Updates create a new row in `account_details`
- Deletes create a row in `deleted_accounts`
- Rows in `accounts` are not hard-deleted

Useful SQL checks:

```sql
SELECT * FROM accounts;
SELECT * FROM account_types;
SELECT * FROM account_details ORDER BY timestamp DESC;
SELECT * FROM deleted_accounts ORDER BY timestamp DESC;
```