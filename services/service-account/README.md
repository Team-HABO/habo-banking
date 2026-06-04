# Service Account

Django microservice responsible for account commands (create, update, freeze, delete, transaction start, exchange start).

**Stack:** Python 3.12 · Django 5.0.1 · Django REST Framework · PostgreSQL · RabbitMQ · PyJWT

---

## Architecture

| File | Responsibility |
|---|---|
| `accounts/views.py` | HTTP handlers, JWT extraction |
| `accounts/serializers.py` | Request validation + response shaping |
| `accounts/services.py` | Business logic, duplicate guard |
| `accounts/publishers.py` | RabbitMQ event publishing |
| `accounts/models.py` | ORM models (`Account`, `AccountDetail`, `AccountType`, `DeletedAccount`) |
| `account_service/urls.py` | Root routing — mounts endpoints under `/v1/accounts/` |
| `account_service/settings.py` | Django settings, JWT config, Swagger config |

---

## Endpoints

All endpoints are rooted at `/v1/accounts/`.

| Method | Endpoint | Contract | Auth |
|---|---|---|---|
| POST | `/v1/accounts/` | 1 – Create account | JWT required |
| PATCH | `/v1/accounts/{guid}/` | 2 – Freeze / unfreeze | None |
| PUT | `/v1/accounts/{guid}/` | 3 – Rename / change type | None |
| DELETE | `/v1/accounts/{guid}/` | 4 – Soft delete | None |
| POST | `/v1/accounts/{guid}/transactions/` | 5 – Initiate transaction | None |
| POST | `/v1/accounts/{guid}/exchanges/` | 6 – Initiate currency exchange | None |

### Swagger UI / OpenAPI

A live Swagger UI is served at `/api/docs/` and the raw OpenAPI 3.0 schema at `/api/schema/`.

---

## Authentication

`POST /v1/accounts/` reads `owner_id` exclusively from a JWT. The token is expected either as:
- `Authorization: Bearer <token>` header, or
- `auth_token` cookie (set by `service-auth`)

The JWT must be signed with HS256 using the secret in `JWT_SECRET_KEY`. The `nameid` claim is used as `owner_id`.

---

## Example request bodies

**Create account** (`POST /v1/accounts/`)
```json
{
  "name": "My Savings",
  "type": "savings"
}
```

**Freeze / unfreeze** (`PATCH /v1/accounts/{guid}/`)
```json
{
  "freeze": true
}
```

**Rename / change type** (`PUT /v1/accounts/{guid}/`)
```json
{
  "name": "Daily Account",
  "type": "checking"
}
```

**Transaction** (`POST /v1/accounts/{guid}/transactions/`)
```json
{
  "amount": "500",
  "transactionType": "DEPOSIT",
  "messageId": "550e8400-e29b-41d4-a716-446655440000"
}
```
> `transactionType` must be one of: `DEPOSIT`, `WITHDRAW`, `TRANSFER`.  
> `receiverAccountGuid` is required when `transactionType` is `TRANSFER`.

**Exchange** (`POST /v1/accounts/{guid}/exchanges/`)
```json
{
  "amount": "100",
  "currency": "EUR",
  "messageId": "40ff6f1a-3b84-43a6-b440-d15918f5bc64"
}
```

---

## RabbitMQ publishing

| Exchange | Type | Routing key | Published on |
|---|---|---|---|
| `account-exchange-events` | fanout | — | Create, Delete |
| `synchronize-events` | direct | `synchronize-account-queue` | Create, Freeze, Update, Delete |
| `ai-service-transaction` | fanout | — | Transaction, Exchange |

---

## Database model

The service uses an **immutable history pattern** — rows are never mutated or hard-deleted:

- Every state change (rename, freeze) creates a new `AccountDetail` row
- Deletes create a `DeletedAccount` row; the `Account` row is preserved
- The current state of an account is always the latest `AccountDetail` by timestamp

**Tables:** `accounts` · `account_types` · `account_details` · `deleted_accounts`

Useful SQL:
```sql
SELECT * FROM accounts;
SELECT * FROM account_types;
SELECT * FROM account_details ORDER BY timestamp DESC;
SELECT * FROM deleted_accounts ORDER BY timestamp DESC;
```

---

## Running locally (dev container)

1. Open the folder in VS Code and select **Reopen in Container**
2. In the devcontainer terminal:

```bash
python manage.py migrate
python manage.py runserver 0.0.0.0:8000
```

Port `8000` is forwarded automatically. Open:
- API: `http://localhost:8000/v1/accounts/`
- Swagger UI: `http://localhost:8000/api/docs/`
- RabbitMQ UI: `http://localhost:15672`

---

## Environment variables

| Variable | Description |
|---|---|
| `POSTGRES_DB` | Database name |
| `POSTGRES_USER` | Database user |
| `POSTGRES_PASSWORD` | Database password |
| `POSTGRES_HOST` | Database host (default: `db`) |
| `RABBITMQ_HOST` | RabbitMQ host (default: `localhost`) |
| `RABBITMQ_PORT` | RabbitMQ port (default: `5672`) |
| `RABBITMQ_USER` | RabbitMQ user (default: `guest`) |
| `RABBITMQ_PASSWORD` | RabbitMQ password |
| `JWT_SECRET_KEY` | HS256 secret used to verify `auth_token` JWTs |
