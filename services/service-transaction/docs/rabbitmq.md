# RabbitMQ

## Configuration

The service connects to RabbitMQ using the default configuration:

- **Host**: `rabbitmq` (Not 'localhost', since we are running inside a Dev Container)
- **Exchange**: `service_ai.Messages:FraudChecked` (fanout)
- **Queue**: `check-fraud`
- **Protocol**: AMQP

Connection settings should not be modified for this application to run in a Dev Container. Nevertheless, to modify the connection settings, update [../src/RabbitMQ.ts](../src/RabbitMQ.ts).

## Usage

### Running the Consumer

Listen for transaction messages from the `service_ai.Messages:FraudChecked` exchange:

```bash
npm run consume
```

Output:

```bash
RabbitMQ connected and channel created successfully.
```

The consumer will route incoming messages to the appropriate handler based on transaction type (deposit, withdraw, transfer), acknowledging on success or requeuing on failure.

### Running the Producer

Send a test message to the `hello-queue` queue:

```bash
npm run produce
```

Output:

```bash
RabbitMQ connected and channel created successfully.
[X] Sent { message: 'Hello World!' }
RabbitMQ connection closed.
```
