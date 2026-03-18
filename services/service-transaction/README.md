# Service Transaction

A microservice for handling financial transactions (deposits, withdrawals, transfers) using RabbitMQ message queue and PostgreSQL with Prisma ORM.

## Overview

- **Consumer**: Listens for fraud-checked transaction messages from the `service_ai.Messages:FraudChecked` fanout exchange via the `check-fraud` queue, and routes them to the appropriate handler (deposit, withdraw, transfer).
- **Producer**: A standalone demo script that sends a test message to a `hello-queue` queue.
- **RabbitMQ**: Generic connection and channel management class supporting both direct queue and exchange-based messaging.

## Prerequisites

- Docker
- VS Code with [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension

## Installation

1. Setup `.env` in `.devcontainer` directory (see `.env-sample` for reference).
2. Run `SHIFT + CTRL + P` (`SHIFT + CMD + P` in MacOS) and type `>Dev Containers: Rebuild and Reopen in Container`.
3. Once inside the container, run `npm install` to install npm dependencies.
4. Run `npm run generate` to generate Prisma Client.
5. Run `npm run migrate` to apply database migrations.
6. To run the application, see [rabbitmq.md](./docs/rabbitmq.md).
