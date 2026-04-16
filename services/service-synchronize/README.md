# Service Synchronize

Service Synchronize is the read-model updater in the CQRS setup for Habo Banking.
It consumes events from RabbitMQ and writes denormalized user/account/transaction state to MongoDB.

## Responsibilities

- Consume `account.created` and `transaction.created` events from exchange `synchronize-events`.
- Validate incoming message structure and message types.
- Apply idempotency checks for transactions using `metadata.messageId`.
- Persist account and transaction changes in MongoDB.
- Execute transfer updates as a MongoDB transaction (sender + receiver update together).

## Tech Stack

- .NET 10
- MassTransit + RabbitMQ
- MongoDB
- xUnit + Moq + Testcontainers

## Prerequisites

- .NET SDK 10
- Docker Desktop

MongoDB must run as a replica set because transfer processing uses MongoDB transactions.

## Configuration

The service reads environment variables first and falls back to local defaults.

- `MONGODB_CONNECTION_STRING` (default: `mongodb://localhost:27017`)
- `RABBITMQ_HOST` (default: `localhost`)

`DotNetEnv` is enabled, so values can also be loaded from a `.env` file in parent folders.

## Run With Docker Compose

From this folder (`services/service-synchronize`):

```powershell
docker compose up --build
```

This starts:

- MongoDB (with replica set init via healthcheck)
- RabbitMQ (management UI on `http://localhost:15672`)
- `service-synchronize` with `dotnet watch run`

Stop everything:

```powershell
docker compose down
```

## Run Locally (Service Outside Docker)

1. Start MongoDB replica set:

```powershell
docker run -d --name mongo_replica -p 27017:27017 mongo:7 mongod --replSet rs0 --bind_ip_all
docker exec -it mongo_replica mongosh --eval "rs.initiate({_id: 'rs0', members: [{_id: 0, host: 'localhost:27017'}]})"
```

2. Start RabbitMQ:

```powershell
docker run -d --name rabbitmq_sync -p 5672:5672 -p 15672:15672 rabbitmq:4-management-alpine
```

3. Start the service:

```powershell
Set-Location .\service-synchronize
$env:MONGODB_CONNECTION_STRING="mongodb://localhost:27017/?replicaSet=rs0&directConnection=true"
$env:RABBITMQ_HOST="localhost"
dotnet run
```

## Queue and Routing Setup

- Exchange: `synchronize-events` (type: `direct`)
- Queue: `synchronize-account-queue` bound with routing key `account.created`
- Queue: `synchronize-transaction-queue` bound with routing key `transaction.created`

## Message Contracts

### Account Created (`synchronize-account`)

```json
{
    "data": {
        "ownerId": "user-1",
        "account": {
            "accountGuid": "1",
            "type": "savings",
            "name": "my savings",
            "isFrozen": false,
            "timestamp": "2026-04-06T09:21:00Z",
            "balance": {
                "amount": "0"
            }
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_CREATE",
        "messageTimestamp": "2026-04-06T09:22:00Z"
    }
}
```

### Transaction Deposit (`synchronize-transaction`)

```json
{
    "data": {
        "ownerId": "user-1",
        "account": {
            "guid": "1",
            "audit": {
                "amount": "200.00",
                "type": "DEPOSIT",
                "timestamp": "2026-04-06T09:22:00Z"
            }
        }
    },
    "metadata": {
        "messageType": "DEPOSIT",
        "messageTimestamp": "2026-04-06T09:22:00Z",
        "messageId": "GUID"
    }
}
```

### Transaction Withdraw (`synchronize-transaction`)

```json
{
    "data": {
        "ownerId": "user-1",
        "account": {
            "guid": "1",
            "audit": {
                "amount": "20.50",
                "type": "WITHDRAW",
                "timestamp": "2026-04-06T09:22:00Z"
            }
        }
    },
    "metadata": {
        "messageType": "WITHDRAW",
        "messageTimestamp": "2026-04-06T09:22:00Z",
        "messageId": "GUID2"
    }
}
```

### Transaction Transfer (`synchronize-transaction`)
remember to create another user

```json
{
    "data": {
        "ownerId": "user-1",
        "account": {
            "guid": "1",
            "audit": {
                "receiver" : "my savings user 2",
                "amount": "20",
                "type": "TRANSFER",
                "timestamp": "2026-04-06T09:22:00Z"
            }
        },
        "receiver": {
            "guid": "2",
            "audit": {
                "receiver": "my savings",
                "amount": "20",
                "type": "TRANSFER",
                "timestamp": "2026-04-06T09:22:00Z"
            }
        }
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER",
        "messageTimestamp": "2026-04-06T09:22:00Z",
        "messageId": "GUID44"
    }
}
```

## Testing

Run all tests:

```powershell
dotnet test .\service-synchronize.tests\service-synchronize.tests.csproj
```

Run only transaction unit tests:

```powershell
dotnet test .\service-synchronize.tests\service-synchronize.tests.csproj --filter "FullyQualifiedName~TransactionServiceTests"
```

Note: integration tests depend on Docker/Testcontainers availability.

## Troubleshooting

- Mongo connection refused on `localhost:27017`:
  - Ensure Mongo container is running and replica set is initialized.
- Messages not consumed:
  - Check RabbitMQ bindings for exchange `synchronize-events` and routing keys.
- Duplicate transaction message:
  - The service skips already-processed messages when `metadata.messageId` already exists in audits.
- VS/OmniSharp package errors after container watch run:
  - Keep `bin`, `obj`, and NuGet cache on container-only volumes (already configured in `docker-compose.yml`).
