# Service AI

Fraud detection microservice for the Habo Banking platform. Consumes transaction metadata from RabbitMQ, sends it to an AI model via the **OpenRouter API**, and publishes a fraud risk assessment.

**Stack:** .NET 9 · MassTransit · RabbitMQ · Serilog · OpenRouter

## Architecture

```
Account Service ──► RabbitMQ (CheckFraud) ──► AI Service ──► OpenRouter API
                                                    │
                                      ┌─────────────┴──────────────┐
                                      ▼                            ▼
                           RabbitMQ (FraudChecked)     RabbitMQ (FraudNotification)
                            (no fraud detected)          (fraud detected / error)
                                      │                            │
                                      ▼                            ▼
                            Transaction Service          Notification Service
```

## Flow (Contract ID 5 — Bank Transaction)

| Outcome                             | Published message   | Destination                                               |
|-------------------------------------|---------------------|-----------------------------------------------------------|
| No fraud detected                   | `FraudChecked`      | Transaction-Service (step 3)                              |
| Fraud detected                      | `FraudNotification` | Notification-Service (step 2.5)                           |
| AI service error / unexpected error | `FraudNotification` | Notification-Service (transaction blocked on uncertainty) |

## Risk Heuristics

The AI model flags transactions matching these rules:

1. **Threshold Violation** — Amount exceeds 10,000.
2. **Geographical Risk** — Origin IP address originates from a high-risk country: India, Nigeria, Romania, Vietnam, or Brazil.

## Messages

### CheckFraud (consumed)

Sent by the Account-Service (contract ID 5, step 2). Contains `data` with `account` (guid, name, type), optional `receiver` (transfer only), `amount`, `transactionType`, and `originIpAddress`. `metadata.messageType` is one of `TRANSACTION_TRANSFER` / `TRANSACTION_WITHDRAW` / `TRANSACTION_DEPOSIT`.

### FraudChecked (published — step 3)

Published to the Transaction-Service when no fraud is detected. Passes `data` (account, receiver, amount, transactionType) and `metadata` through unchanged for the Transaction-Service to process.

### FraudNotification (published — step 2.5)

Published to the Notification-Service when fraud is detected or the AI service fails (transaction blocked on uncertainty). Contains `data.message` with a human-readable reason and `metadata.messageType` passed through from `CheckFraud`.

## Configuration

Set the following environment variables in a `.env` file at the project root (loaded via `DotNetEnv`):

| Variable             | Description                      |
|----------------------|----------------------------------|
| `OPENROUTER_API_KEY` | OpenRouter API key               |
| `RABBITMQ_USERNAME`  | RabbitMQ username                |
| `RABBITMQ_PASSWORD`  | RabbitMQ password                |
| `RABBITMQ_HOST`      | RabbitMQ host (e.g. `localhost`) |

The AI model is configured via `appsettings.json`:

| Key                | Description      |
|--------------------|------------------|
| `OpenRouter:Model` | Model identifier |

## Running

```bash
# Start RabbitMQ
docker compose up -d

# Run the service
dotnet run
```

## Testing via RabbitMQ UI

1. Open `http://localhost:15672` (login `guest`/`guest`).
2. Go to **Queues** → find the `check-fraud` queue → **Publish message**.
3. Paste a test payload:

Deposit example:

```json
{
 "messageType": ["urn:message:service_ai.Messages:CheckFraud"],
 "message": {
  "data": {
   "account": {
    "guid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Main Account",
    "type": "checking"
   },
   "amount": "15000",
   "transactionType": "deposit",
   "originIpAddress": "185.93.2.100"
  },
  "metadata": {
   "messageType": "TRANSACTION_DEPOSIT",
   "messageTimestamp": "2026-03-08T12:00:00Z"
  }
 }
}
```

Transfer example (includes receiver):

```json
{
 "messageType": ["urn:message:service_ai.Messages:CheckFraud"],
 "message": {
  "data": {
   "account": {
    "guid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Main Account",
    "type": "checking"
   },
   "receiver": {
    "guid": "8a1bc234-1234-5678-abcd-1234567890ab",
    "name": "Savings Account",
    "type": "savings"
   },
   "amount": "500",
   "transactionType": "transfer",
   "originIpAddress": "80.71.142.50"
  },
  "metadata": {
   "messageType": "TRANSACTION_TRANSFER",
   "messageTimestamp": "2026-03-08T12:00:00Z"
  }
 }
}
```

### Test Cases

| # | Scenario              | Amount  | IP                       | Expected                      |
|---|-----------------------|---------|--------------------------|-------------------------------|
| 1 | Threshold violation   | `25000` | `80.71.142.50`           | `FraudNotification` published |
| 2 | Geographical risk     | `500`   | `103.21.244.15` (India)  | `FraudNotification` published |
| 3 | Clean transaction     | `750`   | `185.93.2.100`           | `FraudChecked` published      |
| 4 | Multiple risk factors | `50000` | `49.36.128.42` (Nigeria) | `FraudNotification` published |

## File Structure

```
service-ai/
├── Consumers/
│   └── CheckFraudConsumer.cs         # Consumes CheckFraud, calls AI, routes to FraudChecked or FraudNotification
├── Messages/
│   ├── CheckFraud.cs                 # Input message (consumed from Transaction-Service)
│   ├── FraudChecked.cs               # Output message — no fraud, pass through to Transaction-Service (step 3)
│   ├── FraudNotification.cs          # Output message — fraud detected or AI error, sent to Notification-Service (step 2.5)
│   └── Shared.cs                     # Shared types (AccountInfo)
├── Models/
│   ├── FraudCheckResult.cs           # AI response DTO
│   └── OpenRouterDtos.cs             # OpenRouter API request/response DTOs
├── Services/
│   ├── OpenRouterService.cs          # OpenRouter API client
│   └── Prompts.cs                    # System prompt definitions
├── Program.cs                        # Host & dependency configuration
├── compose.yaml                      # Docker Compose (RabbitMQ)
└── docs/
    └── README.md
```
