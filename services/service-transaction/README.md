# Service Transaction

A microservice for handling events using RabbitMQ message queue.

## Overview

- **Producer**: Sends transaction messages to a RabbitMQ queue, `hello-queue`
- **Consumer**: Listens for and processes transaction messages from the queue, `hello-queue`
- **RabbitMQ**: Centralized connection management for RabbitMQ

## Prerequisites

- Docker
- vscode with [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension

## Installation

1. Run `SHIFT + CTRL + P` (`SHIFT + CMD + P` in MacOS) and type `>Dev Containers: Rebuild and Reopen in Container`
2. Once inside the container, run `npm install` to install node dependencies.

## Configuration

The service connects to RabbitMQ using the default configuration:

- **Host**: `rabbitmq` (Not 'localhost', since we are running inside a Dev Container)
- **Queue**: `hello-queue`
- **Protocol**: AMQP

Connection settings should not be modified for this application to run in a Dev Container. Nevertheless, to modify the connection settings, update [src/utils/RabbitMQ.ts](src/utils/RabbitMQ.ts).

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
