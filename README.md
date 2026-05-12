# Habo Banking
[![AuthService CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-auth.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-auth.yml)
[![Service View CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-view.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-view.yml)
[![Synchronize Service CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-synchronize.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-synchronize.yml)
[![Frontend CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/frontend.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/frontend.yml)
[![Service Account CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-account.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-account.yml)
[![Service AI CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-ai.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-ai.yml)
[![Service Transaction CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-transaction.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-transaction.yml)
[![Service Notification CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-notification.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-notification.yml)
[![Service Currency Exchange CI/CD](https://github.com/Team-HABO/habo-banking/actions/workflows/service-currency-exchange.yml/badge.svg)](https://github.com/Team-HABO/habo-banking/actions/workflows/service-currency-exchange.yml)

A distributed microservices banking application built with an event-driven architecture. The system supports account management, transactions, currency exchange, AI-based fraud detection, and real-time notifications.

## Architecture

The platform follows **CQRS** (Command Query Responsibility Segregation) and the **Saga pattern** for distributed transactions. Services communicate asynchronously via **RabbitMQ** using **MassTransit**.

## Services

| Service                       | Description                                             | Tech                           |
| ----------------------------- | ------------------------------------------------------- | ------------------------------ |
| **service-account**           | Account commands (create, update, freeze, transactions) | Python, Django, PostgreSQL     |
| **service-auth**              | Google OAuth authentication, JWT token generation       | ASP.NET Core                   |
| **service-transaction**       | Transaction processing and orchestration                | TypeScript, Prisma, PostgreSQL |
| **service-currency-exchange** | Real-time currency conversion via Frankfurter API       | .NET, MassTransit              |
| **service-ai**                | LLM-based fraud detection via OpenRouter API            | .NET, MassTransit              |
| **service-notification**      | Email notifications for transactions and fraud alerts   | .NET, MassTransit, SMTP        |
| **service-synchronize**       | CQRS read-model updater                                 | .NET, MassTransit, MongoDB     |
| **service-frontend**          | Web application                                         | React, TypeScript, Vite        |

## Infrastructure

- **Databases**: PostgreSQL (accounts, transactions), MongoDB replica set (read model)
- **Messaging**: RabbitMQ with fanout/direct exchanges and dead letter queues
- **Deployment**: Docker Compose (local), Kubernetes with KEDA autoscaling (production)

## Getting Started

See [infrastructure/docker-local/README.md](infrastructure/docker-local/README.md) for local setup and [infrastructure/kubernetes/README.md](infrastructure/kubernetes/README.md) for Kubernetes deployment.
