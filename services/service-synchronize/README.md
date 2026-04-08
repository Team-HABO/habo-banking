# Service Synchronize

This service is a core component of our CQRS (Command Query Responsibility Segregation) architecture. Its primary responsibility is to maintain data consistency by updating the Query Database (MongoDB) based on state changes occurring in the system's command-side services. It listens for account-related events from service-account, and transaction-related events from trans-action service

## RabbitMQ Consumer Test (UI)

Use the RabbitMQ management UI to manually publish a message and verify the consumer flow.

### 1) Payload

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
                "amount": "0",
                "timestamp": "string"
            }
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_CREATE",
        "messageTimestamp": "2026-04-06T09:22:00Z"
    }
}
```

### 2) Routing key

`account.created`

### 3) Properties

In the RabbitMQ UI **Properties** field, add:

```text
content_type=application/json
```

## Quick Notes

- Use ISO-8601 UTC timestamps (for example: `2026-04-06T09:22:00Z`).
- Ensure message `metadata.messageType` matches the event type expected by the consumer.
- If no message is consumed, verify exchange, queue binding, and routing key configuration.
