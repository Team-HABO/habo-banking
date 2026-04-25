﻿# Service Notification

Notification microservice for the Habo Banking platform. Consumes generic notification events from RabbitMQ and
sends alert emails to the relevant recipient via **SMTP**.

**Stack:** .NET 9 · MassTransit · RabbitMQ · MailKit · Serilog

## Architecture

```
AI Service / Other Services ──► notification-events (DIRECT, routing key: notification-queue) ──► Notification Service ──► SMTP Server ──► Email Recipient
```

## RabbitMQ Topology

### Consumed

| Exchange              | Exchange Type | Queue                | Binding Key          | Message        |
| --------------------- | ------------- | -------------------- | -------------------- | -------------- |
| `notification-events` | `direct`      | `notification-queue` | `notification-queue` | `Notification` |

## Flow

| Trigger                 | Action                                              |
| ----------------------- | --------------------------------------------------- |
| `Notification` consumed | Sends an HTML alert email via SMTP to the recipient |

## Messages

### Notification (consumed)

Published by any upstream service (e.g. the AI-Service when fraud is detected, or the currency-exchange service on
failure). Contains `data.message` with a human-readable description and `metadata.messageType` /
`metadata.messageTimestamp` / `metadata.messageId` passed through from the originating service. The
`metadata.messageType` value (e.g. `TRANSACTION_DEPOSIT`, `TRANSACTION_EXCHANGE`) drives the email subject line.

## Configuration

Set the following environment variables in a `.env` file at the project root (loaded via `DotNetEnv`):

| Variable            | Description                       |
| ------------------- | --------------------------------- |
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
2. Go to **Queues** → find the `notification-queue` queue → **Publish message**.
3. Paste a test payload:

```json
{
	"data": {
		"message": "Fraud detected: transaction amount of 25000 exceeds the allowed threshold of 10000."
	},
	"metadata": {
		"messageType": "TRANSACTION_DEPOSIT",
		"messageTimestamp": "2026-03-10T12:00:00Z"
	}
}
```

## File Structure

```
service-notification/
├── Consumers/
│   └── NotificationConsumer.cs  # Consumes Notification and triggers email sending
├── Messages/
│   └── Notification.cs          # Input message (consumed from the notification-events exchange)
├── Services/
│   ├── IEmailService.cs         # Email service interface
│   └── EmailService.cs          # MailKit SMTP email sender implementation
├── Settings/
│   └── EmailSettings.cs         # SMTP configuration settings
├── Program.cs                   # Host & dependency configuration
└── docs/
    └── README.md
```
