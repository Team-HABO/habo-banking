# Service AI

Fraud detection microservice for the Habo Banking platform. Consumes transaction metadata from RabbitMQ, sends it to an AI model via the **OpenRouter API**, and publishes a fraud risk assessment.

**Stack:** .NET 9 · MassTransit · RabbitMQ · Serilog · OpenRouter

## Architecture

```
Account Service ──► ai-service-transaction (FANOUT) ──► ai-transaction-queue ──► AI Service ──► OpenRouter API
                                                                                        │
                                                              ┌─────────────────────────┴──────────────────────┐
                                                              ▼                                               ▼
                                      service_ai.Messages:FraudChecked (FANOUT)     notification-events (DIRECT)
                                               (no fraud detected)                    routing key: notification-queue
                                                              │                                               │
                                                              ▼                                               ▼
                                                  Transaction Service                           Notification Service
```

## RabbitMQ Topology

### Consumed

| Exchange                 | Exchange Type | Queue                  | Binding Key | Message      |
| ------------------------ | ------------- | ---------------------- | ----------- | ------------ |
| `ai-service-transaction` | `fanout`      | `ai-transaction-queue` | —           | `CheckFraud` |

### Published

| Exchange                           | Exchange Type | Routing Key          | Message             | Destination          |
| ---------------------------------- | ------------- | -------------------- | ------------------- | -------------------- |
| `service_ai.Messages:FraudChecked` | `fanout`      | —                    | `FraudChecked`      | Transaction-Service  |
| `notification-events`              | `direct`      | `notification-queue` | `FraudNotification` | Notification-Service |

## Flow (Contract ID 5 — Bank Transaction)

| Outcome                             | Published message   | Destination                                               |
| ----------------------------------- | ------------------- | --------------------------------------------------------- |
| No fraud detected                   | `FraudChecked`      | Transaction-Service (step 3)                              |
| Fraud detected                      | `FraudNotification` | Notification-Service (step 2.5)                           |
| AI service error / unexpected error | `FraudNotification` | Notification-Service (transaction blocked on uncertainty) |

## Risk Heuristics

The AI model flags transactions matching these rules:

1. **Threshold Violation** — Amount exceeds 10,000.
2. **Geographical Risk** — Origin IP address originates from a high-risk country: India, Nigeria, Romania, Vietnam, or Brazil.

## Messages

### CheckFraud (consumed)

Sent by the Account-Service (contract ID 5, step 2). Contains `data` with `ownerId`, `account` (guid, name, type), optional `receiver` (transfer only), `amount`, `transactionType`, and `originIpAddress`. `metadata.messageType` is one of `TRANSACTION_TRANSFER` / `TRANSACTION_WITHDRAW` / `TRANSACTION_DEPOSIT`.

### FraudChecked (published — step 3)

Published to the Transaction-Service when no fraud is detected. Passes `data` (`ownerId`, `account`, `receiver`, `amount`, `transactionType`) and `metadata` through unchanged for the Transaction-Service to process.

### FraudNotification (published — step 2.5)

Published to the Notification-Service when fraud is detected or the AI service fails (transaction blocked on uncertainty). Contains `data.message` with a human-readable reason and `metadata.messageType` passed through from `CheckFraud`.

## Configuration

Set the following environment variables in a `.env` file at the project root (loaded via `DotNetEnv`):

| Variable             | Description                      |
| -------------------- | -------------------------------- |
| `OPENROUTER_API_KEY` | OpenRouter API key               |
| `RABBITMQ_USERNAME`  | RabbitMQ username                |
| `RABBITMQ_PASSWORD`  | RabbitMQ password                |
| `RABBITMQ_HOST`      | RabbitMQ host (e.g. `localhost`) |

The AI model is configured via `appsettings.json`:

| Key                | Description      |
| ------------------ | ---------------- |
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
2. Go to **Queues** → find the `ai-transaction-queue` queue → **Publish message**.
3. Paste a test payload:

#### Deposit Example (Expected Outcome: FRAUD)

Reason: Amount is over 10,000 AND the IP originates from Vietnam (1.52.0.0/14).

```json
{
	"messageType": ["urn:message:service_ai.Messages:CheckFraud"],
	"message": {
		"data": {
			"ownerId": "user-123",
			"account": {
				"guid": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
				"name": "Main Account",
				"type": "checking"
			},
			"amount": "15000",
			"transactionType": "deposit",
			"originIpAddress": "1.52.1.1"
		},
		"metadata": {
			"messageType": "TRANSACTION_DEPOSIT",
			"messageTimestamp": "2026-03-08T12:00:00Z"
		}
	}
}
```

#### Transfer Example (Expected Outcome: CLEAR)

Reason: Amount is under 10,000 and the IP is not in the high-risk country list.

```json
{
	"messageType": ["urn:message:service_ai.Messages:CheckFraud"],
	"message": {
		"data": {
			"ownerId": "user-456",
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
			"originIpAddress": "8.8.8.8"
		},
		"metadata": {
			"messageType": "TRANSACTION_TRANSFER",
			"messageTimestamp": "2026-03-08T12:00:00Z"
		}
	}
}
```

### Test Cases

| #   | Scenario                            | Amount  | Origin IP Address | Expected Outcome  | Reasoning for LLM                                        |
| :-- | :---------------------------------- | :------ | :---------------- | :---------------- | :------------------------------------------------------- |
| 1   | **Threshold Violation**             | `25000` | `8.8.8.8`         | `is_fraud: true`  | Amount exceeds 10,000 threshold.                         |
| 2   | **Geographical Risk (India)**       | `500`   | `103.21.244.15`   | `is_fraud: true`  | IP falls within India's `103.0.0.0/8` range.             |
| 3   | **Clean Transaction**               | `750`   | `1.1.1.1`         | `is_fraud: false` | Amount is low and IP is not in a high-risk range.        |
| 4   | **Multiple Risk Factors (Nigeria)** | `50000` | `41.203.64.1`     | `is_fraud: true`  | Both threshold and Nigeria `41.0.0.0/8` range triggered. |
| 5   | **Geographical Risk (Brazil)**      | `1200`  | `177.42.10.5`     | `is_fraud: true`  | IP falls within Brazil's `177.0.0.0/8` range.            |
| 6   | **Geographical Risk (Romania)**     | `9000`  | `5.2.128.50`      | `is_fraud: true`  | IP falls within Romania's `5.2.0.0/14` range.            |

## File Structure

```
service-ai/
├── Consumers/
│   └── CheckFraudConsumer.cs         # Consumes CheckFraud, calls AI, routes to FraudChecked or FraudNotification
├── Messages/
│   ├── CheckFraud.cs                 # Input message — published by Account-Service
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
