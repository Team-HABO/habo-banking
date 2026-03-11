# Service Notification

Fraud notification microservice for the Habo Banking platform. Consumes fraud notification events from RabbitMQ and
sends alert emails to the relevant recipient via **SMTP**.

**Stack:** .NET 9 · MassTransit · RabbitMQ · MailKit · Serilog

## Architecture

```
AI Service ──► RabbitMQ (FraudNotification) ──► Notification Service ──► SMTP Server ──► Email Recipient
```

## Flow (Contract ID 5 — Bank Transaction, Step 2.5)

| Trigger                      | Action                                              |
|------------------------------|-----------------------------------------------------|
| `FraudNotification` consumed | Sends an HTML alert email via SMTP to the recipient |

## Messages

### FraudNotification (consumed)

Published by the AI-Service (contract ID 5, step 2.5) when fraud is detected or the AI service fails. Contains
`data.message` with a human-readable fraud reason and `metadata.messageType` / `metadata.messageTimestamp` passed
through from the original `CheckFraud` message.

## Configuration

Set the following environment variables in a `.env` file at the project root (loaded via `DotNetEnv`):

| Variable            | Description                       |
|---------------------|-----------------------------------|
| `RABBITMQ_USERNAME` | RabbitMQ username                 |
| `RABBITMQ_PASSWORD` | RabbitMQ password                 |
| `RABBITMQ_HOST`     | RabbitMQ host (e.g. `localhost`)  |
| `SMTP_HOST`         | SMTP server host                  |
| `SMTP_PORT`         | SMTP server port (default: `587`) |
| `SMTP_USERNAME`     | SMTP authentication username      |
| `SMTP_PASSWORD`     | SMTP authentication password      |
| `SMTP_FROM_EMAIL`   | Sender email address              |
| `SMTP_FROM_NAME`    | Sender display name               |

## Running

```bash
# Run the service
dotnet run
```

## Testing via RabbitMQ UI

1. Open `http://localhost:15672` (login `guest`/`guest`).
2. Go to **Queues** → find the `fraud-notification` queue → **Publish message**.
3. Paste a test payload:

```json
{
  "messageType": ["urn:message:habo.banking:FraudNotification"],
  "message": {
    "data": {
      "message": "Fraud detected: transaction amount of 25000 exceeds the allowed threshold of 10000."
    },
    "metadata": {
      "messageType": "TRANSACTION_DEPOSIT",
      "messageTimestamp": "2026-03-10T12:00:00Z"
    }
  }
}
```

## File Structure

```
service-notification/
├── Consumers/
│   └── FraudNotificationConsumer.cs  # Consumes FraudNotification and triggers email sending
├── Messages/
│   └── FraudNotification.cs          # Input message (consumed from AI-Service)
├── Services/
│   ├── IEmailService.cs              # Email service interface
│   └── EmailService.cs               # MailKit SMTP email sender implementation
├── Settings/
│   └── EmailSettings.cs              # SMTP configuration settings
├── Program.cs                        # Host & dependency configuration
└── docs/
    └── README.md
```

