# Service Currency Exchange

Currency exchange microservice for the Habo Banking platform. Consumes exchange requests from RabbitMQ, fetches the live DKK → target currency rate from the **Frankfurter API**, and publishes the result back to the Transaction-Service or a failure notification to the Notification-Service.

**Stack:** .NET 9 · MassTransit · RabbitMQ · Frankfurter API · Serilog

## Architecture

```
Transaction Service ──► currency-exchange-events (DIRECT) ──► currency-exchange-requests-queue ──► Currency Service ──► Frankfurter API
                                                                                                            │
                                                                   ┌──────────────────────────┴─────────────────────────┐
                                                                   ▼                                                       ▼
                                      currency-exchange-events (DIRECT, routing key: currency-exchange-response-queue)     notification-events (DIRECT, routing key: notification-queue)
                                                   (→ Transaction-Service)                                                        (→ Notification-Service)
```

## RabbitMQ Topology

### Consumed

| Exchange                   | Exchange Type | Queue                              | Binding Key                        | Message             |
|----------------------------|---------------|------------------------------------|------------------------------------|---------------------|
| `currency-exchange-events` | `direct`      | `currency-exchange-requests-queue` | `currency-exchange-requests-queue` | `ExchangeRequested` |

### Published

| Exchange                   | Exchange Type | Routing Key                        | Message                | Destination          |
|----------------------------|---------------|------------------------------------|------------------------|----------------------|
| `currency-exchange-events` | `direct`      | `currency-exchange-response-queue` | `ExchangeProcessed`    | Transaction-Service  |
| `notification-events`      | `direct`      | `notification-queue`               | `ExchangeNotification` | Notification-Service |

## Flow (Contract ID 6 — Currency Exchange)

| Outcome                            | Published message      | Destination                     |
|------------------------------------|------------------------|---------------------------------|
| Rate resolved successfully         | `ExchangeProcessed`    | Transaction-Service (step 4)    |
| Unsupported currency / API failure | `ExchangeNotification` | Notification-Service (step 4.4) |

## Messages

### ExchangeRequested (consumed)

Published by the Transaction-Service (contract ID 6, step 3). Contains `data` with `ownerId`, `accountGuid`, `amount`, `currency` (target currency to exchange DKK into), and `transactionType`. `metadata.messageType` is `TRANSACTION_EXCHANGE`.

### ExchangeProcessed (published — step 4)

Published to the Transaction-Service when the exchange rate is successfully resolved. Carries all original `data` fields (`ownerId`, `accountGuid`, `amount`, `currency`, `transactionType`) plus `exchangeRate` (DKK → target currency as a `double`) and `metadata` passed through from `ExchangeRequested`.

### ExchangeNotification (published — step 4.4)

> Published to the Notification-Service when the exchange rate cannot be retrieved (unsupported currency or API error). Contains `data.message` with a human-readable reason and `metadata` passed through from `ExchangeRequested`. The message shape (`data.message` + `metadata`) is identical to the `FraudNotification` published by the AI-Service.

## Configuration

Set the following environment variables in a `.env` file at the project root (loaded via `DotNetEnv`):

| Variable            | Description                      |
|---------------------|----------------------------------|
| `RABBITMQ_USERNAME` | RabbitMQ username                |
| `RABBITMQ_PASSWORD` | RabbitMQ password                |
| `RABBITMQ_HOST`     | RabbitMQ host (e.g. `localhost`) |

Frankfurter is a free, open-source API — no API key required. The base URL is configured via `appsettings.json`:

| Key                   | Description              |
|-----------------------|--------------------------|
| `Frankfurter:BaseUrl` | Frankfurter API base URL |

## Running

```bash
# Start the service (builds and runs the service-currency-exchange container)
docker compose up -d

# Run the service locally (requires RabbitMQ to be running separately, e.g. via a shared root-level compose)
dotnet run
```

> **Note:** The `compose.yaml` in this directory builds and runs the `service-currency-exchange` image only — it does not include a RabbitMQ service. Make sure RabbitMQ is available (e.g. started from the root-level compose or another compose file) before running the service.

## Testing via RabbitMQ UI

1. Open `http://localhost:15672` (login `guest`/`guest`).
2. Go to **Queues** → find the `currency-exchange-requests-queue` queue → **Publish message**.
3. Paste a test payload:

Successful exchange (DKK → USD):

```json
{
 "data": {
  "ownerId": "user-123",
  "accountGuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountName": "My Savings",
  "amount": "1000",
  "currency": "USD",
  "transactionType": "exchange"
 },
 "metadata": {
  "messageType": "TRANSACTION_EXCHANGE",
  "messageTimestamp": "2026-03-12T12:00:00Z",
  "messageId": "d3b07384-d113-4ec4-a1e0-b3cc7c9c6e1a"
 }
}
```

Unsupported currency (triggers `ExchangeNotification`):

```json
{
 "data": {
  "ownerId": "user-456",
  "accountGuid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "accountName": "My Savings",
  "amount": "500",
  "currency": "XYZ",
  "transactionType": "exchange"
 },
 "metadata": {
  "messageType": "TRANSACTION_EXCHANGE",
  "messageTimestamp": "2026-03-12T12:00:00Z",
  "messageId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
 }
}
```

### Test Cases

| # | Scenario                        | Currency | Expected                         |
|---|---------------------------------|----------|----------------------------------|
| 1 | Valid target currency           | `USD`    | `ExchangeProcessed` published    |
| 2 | Unsupported currency            | `XYZ`    | `ExchangeNotification` published |
| 3 | Service throws unexpected error | `EUR`    | `ExchangeNotification` published |

## File Structure

```
service-currency-exchange/
├── Consumers/
│   └── ExchangeRequestedConsumer.cs  # Consumes ExchangeRequested, calls Frankfurter, routes result
├── Messages/
│   ├── ExchangeRequested.cs          # Input message — published by Transaction-Service (step 3)
│   ├── ExchangeProcessed.cs          # Output message — rate resolved, sent to Transaction-Service (step 4)
│   └── ExchangeNotification.cs       # Output message — failure, sent to Notification-Service (step 4.4)
├── Models/
│   └── FrankfurterDtos.cs            # Frankfurter API response DTOs
├── Services/
│   └── CurrencyService.cs            # Frankfurter API client
├── service-currency-exchange.Tests.Integration/
│   ├── CurrencyServiceTests.cs       # Integration tests for CurrencyService (real HTTP calls to Frankfurter)
│   ├── ExchangeRequestedConsumerTests.cs  # Integration tests for the consumer (real RabbitMQ via Testcontainers)
│   ├── CurrencyServiceFixture.cs     # xUnit fixture for CurrencyService
│   └── ExchangeRequestedConsumerFixture.cs  # xUnit fixture for the consumer (Testcontainers + real RabbitMQ broker)
├── Program.cs                        # Host & dependency configuration
├── compose.yaml                      # Docker Compose (builds & runs the service image)
└── docs/
    └── README.md
```
