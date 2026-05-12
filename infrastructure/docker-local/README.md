# habo-banking — Local Docker Environment

This directory contains the full local development stack for the habo-banking project. It spins up all application services, supporting infrastructure (databases, message broker), and a complete observability stack (metrics, logs, dashboards) with a single `docker compose up`.

---

## Prerequisites

- Docker Desktop (or Docker Engine + Compose plugin)
- Copy `.env.example` to `.env` and fill in any missing values before the first run

---

## Quick Start

```bash
# First run or after a dependency change
docker compose up --build -d

# Tear everything down (including volumes)
docker compose down -v --remove-orphans
```

After startup, all services should pass their health checks within ~30 seconds. Check status with:

```bash
docker compose ps
```

---

## Service Map

| Service | Technology | Port(s) |
|---|---|---|
| service-account-app | Django (Python) | 8000 |
| service-account-consumer | Django event consumer | — |
| service-ai | C# / .NET 9 | — |
| service-notification | C# / .NET 9 | — |
| service-currency-exchange | C# / .NET 9 | — |
| service-transaction | TypeScript / Node.js (Prisma) | — |
| service-synchronize | C# / .NET (hot-reload) | — |

---

## Infrastructure Services

| Service | Purpose | Port(s) |
|---|---|---|
| RabbitMQ | Message broker | 5672 (AMQP), 15672 (management UI) |
| PostgreSQL (account) | Account service database | 5432 |
| PostgreSQL (transaction) | Transaction service database | 5433 |
| MongoDB | Document store (replica set) | 27017 |
| Mongo Express | MongoDB web UI | 8081 |

**Credentials** (development only — see `.env`):
- PostgreSQL: `postgres / postgres`
- RabbitMQ: `guest / guest`

---

## Observability Stack

The observability stack follows the standard Grafana LGTM pattern: **Loki** for logs, **Prometheus** for metrics, and **Grafana** for visualization. **Grafana Alloy** acts as the local collector agent.

```
Application containers
        │
        ▼
  Grafana Alloy          ← reads container stdout/stderr via Docker socket
  (log collector)
        │
        ▼
     Loki :3100          ← stores and indexes log streams
        │
        ▼
    Grafana :3030         ← dashboards (datasources auto-provisioned)

Application containers
        │  (expose /metrics endpoints)
        ▼
  Prometheus :9090        ← scrapes metrics on a fixed interval
        │
        ▼
    Grafana :3030         ← same Grafana instance, second datasource
```

### Grafana — `http://localhost:3030`

Central UI for both logs and metrics. Two datasources are pre-provisioned automatically at startup (no manual configuration needed):

| Datasource | Type | URL (internal) |
|---|---|---|
| Loki (default) | Logs | `http://loki:3100` |
| Prometheus | Metrics | `http://prometheus:9090` |

A **Services dashboard** is also pre-provisioned and available immediately. It shows per-service panels for:
- Service status (up/down)
- CPU usage
- Working set memory
- Garbage collection rate
- Thread pool thread count and queue length
- Live log tail per service

The dashboard auto-refreshes every 30 seconds and defaults to the last 1 hour of data.

Default Grafana credentials: `admin / admin`.

---

### Prometheus — `http://localhost:9090`

Prometheus scrapes metrics from the three C# services that expose a `/metrics` endpoint:

| Target | Metrics port |
|---|---|
| service-ai | 9091 |
| service-notification | 9092 |
| service-currency-exchange | 9093 |

Config: [prometheus-config.yaml](prometheus-config.yaml)

To verify scrape targets are healthy, open `http://localhost:9090/targets` in a browser.

---

### Loki — `http://localhost:3100`

Loki stores log streams indexed by labels. It is configured for local development:

- **Storage**: filesystem at `/loki/chunks` (persisted in the `loki-data` Docker volume)
- **Index**: TSDB v13 schema with 24-hour index periods
- **KV store**: in-memory (suitable for single-node dev; not for production)

Config: [loki-config.yaml](loki-config.yaml)

Logs are queried via Grafana's Explore view or through the pre-built dashboard. You can filter by the `container` or `service` labels that Alloy attaches to every log line.

---

### Grafana Alloy (log collector)

Alloy runs as a sidecar that reads **all container logs** from the Docker socket and forwards them to Loki. No changes to application code are needed for log collection.

Each log stream is labeled with:
- `container` — the Docker container name
- `service` — the `docker-compose` service name

Config: [alloy-config.alloy](alloy-config.alloy)

This means you can query logs for a specific service in Grafana using:
```
{service="service-account-app"}
```

---

## Grafana Provisioning

Grafana datasources and dashboards are provisioned automatically from the [grafana/provisioning/](grafana/provisioning/) directory — no manual setup needed after `docker compose up`.

```
grafana/provisioning/
├── dashboards/
│   ├── dashboards.yaml      ← tells Grafana where to find dashboard JSON files
│   └── services.json        ← the pre-built Services dashboard
└── datasources/
    ├── loki.yaml            ← Loki datasource (set as default)
    └── prometheus.yaml      ← Prometheus datasource
```

To add a new dashboard, export it as JSON from Grafana and drop the file into `grafana/provisioning/dashboards/`. It will be loaded on the next restart.

---

## Volumes

| Volume | Used by |
|---|---|
| `postgres-account-data` | PostgreSQL (account) |
| `postgres-transaction-data` | PostgreSQL (transaction) |
| `mongodb-data` | MongoDB |
| `loki-data` | Loki (log chunks) |
| `prometheus-data` | Prometheus (TSDB) |
| `grafana-data` | Grafana (state, user dashboards) |
| `synchronize-bin/obj/nuget` | .NET build cache for service-synchronize |

Running `docker compose down -v` removes all volumes, giving you a clean slate. Omit `-v` to preserve data between restarts.

---

## API Testing

A Postman collection covering the main API endpoints is included:

- [habo-banking.postman_collection.json](habo-banking.postman_collection.json)

Import it into Postman or the VS Code Postman extension to start sending requests immediately.
