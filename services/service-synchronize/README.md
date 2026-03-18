to test consumer in rabbit mq UI:

{
  "messageId": "00000000-0000-0000-0000-000000000001",
  "messageType": ["urn:message:service_synchronize.Models:AccountCreated"],
  "message": {
    "data": {
      "accountGuid": "ACC-999-XYZ",
      "type": "Savings",
      "name": "John Doe",
      "isFrozen": false,
      "timestamp": "2026-03-17T20:10:00Z"
    },
    "metadata": {
      "messageType": "ACCOUNT_CREATE",
      "messageTimestamp": "2026-03-17T20:10:00Z"
    }
  }
}
And in the RabbitMQ UI **Properties** field add:
```
content-type: application/vnd.masstransit+json