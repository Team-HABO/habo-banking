# RabbitMQ

## Configuration

The service connects to RabbitMQ using the default configuration:

- **Host**: `rabbitmq` (Not 'localhost', since we are running inside a Dev Container)
- **Protocol**: AMQP

Connection settings should not be modified for this application to run in a Dev Container. Nevertheless, to modify the connection settings, update [../src/RabbitMQ.ts](../src/RabbitMQ.ts).

## Usage

### Running the Consumer

```bash
npm run consume
```

Output:

```bash
RabbitMQ connected and channel created successfully.
```

The consumer will route incoming messages to the appropriate handler based on transaction type (deposit, withdraw, transfer), account creation and deletion, acknowledging on success or requeuing on failure.
