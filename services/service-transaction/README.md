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

1. Setup `.env` in `.devcontainer` directory.
2. Run `SHIFT + CTRL + P` (`SHIFT + CMD + P` in MacOS) and type `>Dev Containers: Rebuild and Reopen in Container`.
3. Once inside the container, run `npm install` to install npm dependencies.
4. Run `npx prisma generate` to generate Prisma Client.
