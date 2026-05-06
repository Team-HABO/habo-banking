# Systems Test — Currency Exchange Flow

End-to-end test that validates the full currency exchange message flow across two live services, using a real RabbitMQ broker and the real [Frankfurter](https://api.frankfurter.dev) exchange-rate API.

## What it tests

The test verifies that a transaction flagged as `EXCHANGE` makes it all the way from the entry point to a confirmed exchange rate:

```
Test runner (acts as AI service)
    │ publishes FraudChecked
    ▼
service-transaction          (Node.js / amqplib)
    │ routes EXCHANGE transactions to currency-exchange service
    ▼
service-currency-exchange    (.NET / MassTransit)
    │ calls Frankfurter API   https://api.frankfurter.dev/v1/
    ▼
ExchangeProcessed message
    │
    ▼
Test assertion: exchangeRate > 0
```

### Why `exchangeRate > 0` proves the HTTP call succeeded

`service-currency-exchange` only publishes `ExchangeProcessed` when Frankfurter returns a valid 2xx response containing a rate. A value greater than zero is therefore a reliable proxy for a successful external API call.

## Prerequisites

- Docker (with Compose V2)
- Node.js ≥ 20
- Internet access (Frankfurter API is called live — no stubs)

## Running manually

### 1. Start the services

From the repo root:

```bash
docker compose -f systems-tests/docker-compose.yml up --build -d
```

This starts:

| Container                   | Purpose                                              |
| --------------------------- | ---------------------------------------------------- |
| `rabbitmq`                  | Message broker (management UI on port 15672)         |
| `postgres`                  | Database for service-transaction's Prisma migrations |
| `service-transaction`       | Consumes `FraudChecked`, routes exchange requests    |
| `service-currency-exchange` | Calls Frankfurter, publishes `ExchangeProcessed`     |

### 2. Wait until service-transaction is ready

The test publishes to a fanout exchange that `service-transaction` must have already bound its `check-fraud` queue to. Poll until the queue appears:

```bash
until curl -s -o /dev/null -w "%{http_code}" -u guest:guest \
  http://localhost:15672/api/queues/%2F/check-fraud | grep -q 200; do
  echo "Waiting for service-transaction..."; sleep 2
done
echo "Ready"
```

Or open the RabbitMQ management UI at http://localhost:15672 (user: `guest` / password: `guest`) and check that the `check-fraud` queue is listed under **Queues**.

### 3. Install dependencies and run the test

```bash
cd systems-tests
npm install   # only needed once / when package.json changes
npm test
```

Expected output:

```
✓ Currency exchange — end-to-end systems test
    ✓ receives ExchangeProcessed with exchangeRate > 0 after calling the real Frankfurter API
```

### 4. Tear down

```bash
docker compose -f systems-tests/docker-compose.yml down
```

## How it works internally

### Test setup (`beforeAll`)

1. Opens an amqplib connection to RabbitMQ.
2. Asserts the `service_ai.Messages:FraudChecked` fanout exchange (safe to call even if it already exists).
3. Asserts the `currency-exchange-events` direct exchange.
4. Declares an **exclusive, auto-delete** queue and binds it to `currency-exchange-events` with routing key `currency-exchange-response-queue`.

The listener queue is created _before_ the message is published, so no `ExchangeProcessed` message can be missed.

### The test

1. Sets up a consumer Promise with a 20-second timeout.
2. Publishes a `FraudChecked` payload directly to the fanout exchange, acting as the AI/fraud-check service:
    ```json
    {
      "data": {
        "transactionType": "EXCHANGE",
        "currency": "USD",
        "amount": "100",
        "ownerId": "systems-test-owner",
        "account": { "guid": "systems-test-account-guid", ... }
      }
    }
    ```
3. Awaits the `ExchangeProcessed` message, then asserts `exchangeRate > 0`.

### RabbitMQ topology

| Exchange                           | Type   | Routing key                        | Consumer                                      |
| ---------------------------------- | ------ | ---------------------------------- | --------------------------------------------- |
| `service_ai.Messages:FraudChecked` | fanout | _(any)_                            | `service-transaction` via `check-fraud` queue |
| `currency-exchange-events`         | direct | `currency-exchange-requests-queue` | `service-currency-exchange`                   |
| `currency-exchange-events`         | direct | `currency-exchange-response-queue` | test runner (exclusive temp queue)            |

## CI

The workflow at [.github/workflows/systems-tests.yml](../.github/workflows/systems-tests.yml) runs these tests automatically. It can also be triggered manually from the **Actions** tab using the `workflow_dispatch` event.
