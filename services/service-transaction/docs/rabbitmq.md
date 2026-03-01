# RabbitMQ

## Configuration

The service connects to RabbitMQ using the default configuration:

- **Host**: `rabbitmq` (Not 'localhost', since we are running inside a Dev Container)
- **Queue**: `hello-queue`
- **Protocol**: AMQP

Connection settings should not be modified for this application to run in a Dev Container. Nevertheless, to modify the connection settings, update [src/RabbitMQ.ts](src/RabbitMQ.ts).

## Usage

### Running the Producer

Send a message to the queue:

```bash
npm run produce
```

Output:

```bash
RabbitMQ connected and channel created successfully.
[X] Sent { message: 'Hello World!' }
RabbitMQ connection closed.
```

### Running the Consumer

In a separate terminal, listen for messages from the queue:

```bash
npm run consume
```

Output:

```bash
RabbitMQ connected and channel created successfully.
 [x] Received:  { message: 'Hello World!' }
 [x] Processing { message: 'Hello World!' }
```
