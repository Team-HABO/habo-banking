# Service AI

Fraud detection microservice for the Habo Banking platform. Consumes transaction metadata from RabbitMQ, sends it to an AI model via the **OpenRouter API**, and publishes a fraud risk assessment.

**Stack:** .NET 9 · MassTransit · RabbitMQ · Serilog · OpenRouter

## Architecture

```
Transaction Service ──► RabbitMQ ──► Service AI ──► OpenRouter API
                                         │
                                         ▼
                                      RabbitMQ (AiProcessResponse)
```

## Risk Heuristics

The AI model flags transactions matching these rules:

1. **Threshold Violation** — Amount exceeds 10,000 (any currency).
2. **Geographical Risk** — Origin IP address is from India.

## Messages

### AiProcessRequest (consumed)

| Field             | Type      |
|-------------------|-----------|
| `Id`              | `Guid`    |
| `SenderAccount`   | `string`  |
| `ReceiverAccount`  | `string`  |
| `Amount`          | `decimal` |
| `Currency`        | `string`  |
| `OriginIpAddress`  | `string`  |

### AiProcessResponse (published)

| Field       | Type     |
|-------------|----------|
| `RequestId` | `Guid`   |
| `IsFraud`   | `bool`   |
| `Reason`    | `string` |
| `RiskScore` | `double` |

## Configuration

Set the following in `appsettings.json` (or environment/user-secrets):

| Key                  | Description             |
|----------------------|-------------------------|
| `OpenRouter:ApiKey`  | OpenRouter API key      |
| `OpenRouter:Model`   | Model identifier        |

RabbitMQ defaults to `localhost` with `guest`/`guest` (see `Program.cs`).

## Running

```bash
# Start RabbitMQ
docker compose up -d

# Run the service
dotnet run
```

## Testing via RabbitMQ UI

1. Open `http://localhost:15672` (login `guest`/`guest`).
2. Go to **Queues** → find the `AiProcessRequest` queue → **Publish message**.
3. Paste a test payload:

```json
{
  "messageType": ["urn:message:service_ai.Messages:AiProcessRequest"],
  "message": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "senderAccount": "DK1234567890",
    "receiverAccount": "DK0987654321",
    "amount": 15000.00,
    "currency": "DKK",
    "originIpAddress": "185.93.2.100"
  }
}
```

### Test Cases

| # | Scenario              | Amount   | IP              | Expected            |
|---|-----------------------|----------|-----------------|---------------------|
| 1 | Threshold violation   | 25,000   | `80.71.142.50`  | `IsFraud: true`     |
| 2 | Geographical risk     | 500      | `103.21.244.15` | `IsFraud: true`     |
| 3 | Clean transaction     | 750      | `185.93.2.100`  | `IsFraud: false`    |
| 4 | Multiple risk factors | 50,000   | `49.36.128.42`  | `IsFraud: true`     |

## File Structure

```
service-ai/
├── Consumers/
│   └── AiProcessRequestConsumer.cs   # Consumes requests, builds prompt, calls AI
├── Messages/
│   ├── AiProcessRequest.cs           # Input message
│   └── AiProcessResponse.cs          # Output message
├── Models/
│   └── FraudCheckResult.cs           # AI response DTO
├── Services/
│   └── OpenRouterService.cs          # OpenRouter API client
├── Program.cs                        # Host & dependency configuration
├── compose.yaml                      # Docker Compose (RabbitMQ)
└── docs/
    └── README.md
```
