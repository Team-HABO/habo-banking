# Service AI - Documentation

## Overview

Service AI is a message-driven microservice that processes AI requests through a RabbitMQ message bus. It uses **MassTransit** as the message bus library and **Serilog** for structured logging.

### Current Architecture (Basic Setup)

```
┌─────────────────┐
│   Other Service │
└────────┬────────┘
         │
    Publishes AiProcessRequest
         │
         ▼
┌──────────────────┐
│    RabbitMQ      │
└────────┬─────────┘
         │
    Subscribes to AiProcessRequest
         │
         ▼
┌──────────────────────────────────┐
│  Service AI                      │
│  AiProcessRequestConsumer        │
│  - Receives request              │
│  - Processes payload             │
│  - Publishes AiProcessResponse   │
└──────────────────────────────────┘
         │
    Publishes AiProcessResponse
         │
         ▼
┌──────────────────┐
│    RabbitMQ      │
└──────────────────┘
```

## Message Types

### AiProcessRequest (Input)
Consumed by the service. Structure:
```csharp
{
    Id: Guid,
    Payload: string
}
```

### AiProcessResponse (Output)
Published by the service. Structure:
```csharp
{
    RequestId: Guid,
    Result: string
}
```

## Testing via RabbitMQ UI

### Prerequisites
- RabbitMQ running (accessible at `http://localhost:15672`)
- Service AI running
- Default credentials: `guest` / `guest`

### Steps to Test

1. **Access RabbitMQ Management UI**
   - Open browser and navigate to: `http://localhost:15672`
   - Login with `guest` / `guest`

2. **Navigate to Queues**
   - Click on the **"Queues"** tab

3. **Find the Service AI Queue**
   - Look for a queue named: `service_ai.Messages:AiProcessRequest` (or similar format based on MassTransit configuration)
   - Click on it to open the queue details

4. **Publish a Test Message**
   - Scroll to **"Publish message"** section
   - Set **Payload** to the following JSON:

```json
{
  "messageType": ["urn:message:service_ai.Messages:AiProcessRequest"],
  "message": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "payload": "hello world"
  }
}
```

5. **Send the Message**
   - Click **"Publish message"**
   - The service should immediately consume and process the message

6. **Verify in Logs**
   - Check the Service AI logs (logs folder or console output):
     ```
     Received request 3fa85f64-5717-4562-b3fc-2c963f66afa6 with payload: hello world
     Published response for request 3fa85f64-5717-4562-b3fc-2c963f66afa6
     ```

7. **View Response (Optional)**
   - Look for the `AiProcessResponse` queue in RabbitMQ
   - The response should contain:
     ```json
     {
       "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
       "result": "Processed: hello world"
     }
     ```

### Example Test Data

**Test Case 1: Simple Processing**
```json
{
  "messageType": ["urn:message:service_ai.Messages:AiProcessRequest"],
  "message": {
    "id": "12345678-1234-1234-1234-123456789012",
    "payload": "test data for AI processing"
  }
}
```

**Test Case 2: Different Payload**
```json
{
  "messageType": ["urn:message:service_ai.Messages:AiProcessRequest"],
  "message": {
    "id": "87654321-4321-4321-4321-210987654321",
    "payload": "another test message"
  }
}
```

## Configuration

### RabbitMQ Connection
Located in `Program.cs`:
```csharp
cfg.Host("localhost", "/", host =>
{
    host.Username("guest");
    host.Password("guest");
});
```

### Logging
- **Minimum Level**: Information
- **Outputs**: 
  - Console
  - File (`logs/.log` with daily rolling)

## File Structure

```
service-ai/
├── Consumers/
│   └── AiProcessRequestConsumer.cs      # Message consumer logic
├── Messages/
│   ├── AiProcessRequest.cs              # Input message type
│   └── AiProcessResponse.cs             # Output message type
├── Program.cs                           # Service configuration
├── Worker.cs                            # Currently unused
├── logs/                                # Log files
└── docs/
    └── README.md                        # This file
```

## Future Changes

This is a basic setup. Expected future changes:
- [ ] Actual AI processing logic implementation
- [ ] Request validation
- [ ] Error handling and dead letter queues
- [ ] Response routing to specific subscribers
- [ ] Timeout handling
- [ ] Metrics and monitoring

## Troubleshooting

**Message not being consumed:**
- Ensure Service AI is running
- Check RabbitMQ connection settings
- Verify queue names match consumer configuration

**No response visible:**
- The response is published to another queue/exchange
- Set up a consumer for `AiProcessResponse` to see the result
- Check the service logs for processing confirmation

