# Dev Container – AI Service

The dev container provides a fully configured, reproducible development environment for the **AI Service**. It runs two containers: the development environment itself (`app`) and a RabbitMQ broker (`rabbitmq`).

---

## Files

```
.devcontainer/
├── devcontainer.json   — VS Code dev container configuration
├── docker-compose.yml  — multi-container definition (app + RabbitMQ)
└── README.md           — this file
```

---

## Containers

### `app`

**Image:** `mcr.microsoft.com/devcontainers/dotnet:9.0`

The container VS Code attaches to. The project root is bind-mounted to `/workspace` inside the container.

**Volumes:**

| Host path                            | Container path            | Notes                                                        |
|--------------------------------------|---------------------------|--------------------------------------------------------------|
| `..` (project root)                  | `/workspace`              | `:cached` is a macOS/Windows perf hint; ignored on Linux     |
| `../../../.git` (repo root `.git`)   | `/workspace/.git`         | Read-only; makes git work across the monorepo mount boundary |
| `~/.gitconfig`                       | `/home/vscode/.gitconfig` | Read-only; shares host Git identity                          |

**Environment variables:**

| Variable             | Source                                             |
|----------------------|----------------------------------------------------|
| `RABBITMQ_HOST`      | Hardcoded to `rabbitmq` (the Compose service name) |
| `OPENROUTER_API_KEY` | Interpolated from `../../../.env` (required)       |

The `.env` file at `../../../.env` (relative to `.devcontainer/`) is required and must exist before the container starts.

**Startup:** The container runs `while sleep 1000; do :; done` to stay alive without starting the application. The application is started manually from the integrated terminal.

**Dependencies:** The `app` service waits for `rabbitmq` to pass its health check before starting.

---

### `rabbitmq`

**Image:** `rabbitmq:4-management-alpine`

Provides the AMQP broker and management UI used by the service during development.

| Setting            | Value                                              |
|--------------------|----------------------------------------------------|
| Default user       | `guest`                                            |
| Default password   | `guest`                                            |
| AMQP port          | `5672`                                             |
| Management UI port | `15672`                                            |
| Data volume        | `rabbitmq-data` (named, persisted across rebuilds) |

**Health check:** `rabbitmq-diagnostics ping`, checked every 10 s with a 20 s start period and 5 retries.

**Management UI:** [http://localhost:15672](http://localhost:15672) — credentials `guest` / `guest`.

---

## Features

Features are installed at image build time.

| Feature                                       | Version                   |
|-----------------------------------------------|---------------------------|
| `ghcr.io/devcontainers/features/git:1`        | `latest` (via Ubuntu PPA) |
| `ghcr.io/devcontainers/features/github-cli:1` | `latest`                  |
| `ghcr.io/devcontainers/features/dotnet:2`     | `9.0`                     |

---

## VS Code extensions

Installed automatically inside the container, not on the host.

| Extension ID                           | Purpose                                             |
|----------------------------------------|-----------------------------------------------------|
| `ms-dotnettools.csdevkit`              | C# Dev Kit (solution explorer, test runner, Roslyn) |
| `ms-dotnettools.csharp`                | C# language server                                  |
| `ms-dotnettools.vscode-dotnet-runtime` | .NET runtime acquisition                            |
| `ms-azuretools.vscode-docker`          | Docker sidebar integration                          |
| `eamodio.gitlens`                      | Inline blame, history, PR integration               |
| `humao.rest-client`                    | Send HTTP requests from `.http` files               |
| `redhat.vscode-xml`                    | `.csproj` / `.xml` syntax and validation            |
| `EditorConfig.EditorConfig`            | Enforces `.editorconfig` rules                      |

---

## VS Code settings

Applied inside the container workspace.

| Setting                                    | Value                   |
|--------------------------------------------|-------------------------|
| `editor.formatOnSave`                      | `true`                  |
| `editor.defaultFormatter`                  | `ms-dotnettools.csharp` |
| `dotnet.defaultSolution`                   | `service-ai.sln`        |
| `terminal.integrated.defaultProfile.linux` | `bash`                  |

---

## Lifecycle

| Hook                | Command          | Runs on                        |
|---------------------|------------------|--------------------------------|
| `postCreateCommand` | `dotnet restore` | Container creation and rebuild |

---

## Port forwarding

| Port    | Label               | Behaviour                             |
|---------|---------------------|---------------------------------------|
| `5672`  | RabbitMQ AMQP       | Forwarded silently                    |
| `15672` | RabbitMQ Management | Forwarded with a VS Code notification |

---

## User

The container runs as the `vscode` user (UID 1000), which is pre-created in the base image and has `sudo` access.

---

## Running the app

From the integrated terminal inside the container:

```bash
dotnet build
dotnet run
```
