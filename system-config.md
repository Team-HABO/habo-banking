# system-config.md

> Project configuration for the IDP SDLC PromptSystem.
> The folder containing this file is the sole source of SDLC configuration.

```yaml
project: HaboBanking
version: 1.0

# Centralized artifacts (relative to this config folder)
artifacts:
    domain-terms: ./docs/artifacts/HaboBanking-domain-terms.md
    domain-concepts: ./docs/artifacts/HaboBanking-domain-concepts.md
    iud: ./docs/artifacts/HaboBanking-intended-use.md
    urs: ./docs/artifacts/HaboBanking-user-requirements.md
    solution-architecture: ./docs/artifacts/HaboBanking-solution-architecture.md
    functional-spec: ./docs/artifacts/HaboBanking.funcspec/
    adrs: ./docs/adrs/
    change-requests: ./docs/change-requests/

# Distributed artifacts (workspace-relative glob patterns)
artifact-patterns:
    container-architecture: 'services/*/docs/*-container-architecture.md'
    openapi-spec: 'services/*/api/*.yaml'

# Service registry
services:
    account:
        repo: habo-banking
        path: ./services/service-account
        container: service-account
        stack: python-django
        container-arch: ./services/service-account/docs/HaboBanking-Account-container-architecture.md

    ai:
        repo: habo-banking
        path: ./services/service-ai
        container: service-ai
        stack: dotnet
        container-arch: ./services/service-ai/docs/HaboBanking-Ai-container-architecture.md

    auth:
        repo: habo-banking
        path: ./services/service-auth
        container: service-auth
        stack: dotnet
        container-arch: ./services/service-auth/docs/HaboBanking-Auth-container-architecture.md

    currency-exchange:
        repo: habo-banking
        path: ./services/service-currency-exchange
        container: service-currency-exchange
        stack: dotnet
        container-arch: ./services/service-currency-exchange/docs/HaboBanking-CurrencyExchange-container-architecture.md

    frontend:
        repo: habo-banking
        path: ./services/service-frontend
        container: service-frontend
        stack: node
        container-arch: ./services/service-frontend/docs/HaboBanking-Frontend-container-architecture.md

    notification:
        repo: habo-banking
        path: ./services/service-notification
        container: service-notification
        stack: dotnet
        container-arch: ./services/service-notification/docs/HaboBanking-Notification-container-architecture.md

    synchronize:
        repo: habo-banking
        path: ./services/service-synchronize
        container: service-synchronize
        stack: dotnet
        container-arch: ./services/service-synchronize/docs/HaboBanking-Synchronize-container-architecture.md

    transaction:
        repo: habo-banking
        path: ./services/service-transaction
        container: service-transaction
        stack: node
        container-arch: ./services/service-transaction/docs/HaboBanking-Transaction-container-architecture.md

    view:
        repo: habo-banking
        path: ./services/service-view
        container: service-view
        stack: node
        container-arch: ./services/service-view/docs/HaboBanking-View-container-architecture.md

# Workflow settings
workflow-settings:
    auto-reflect: true
    reflection-threshold: failure # always | failure | never
    execution-mode: interactive # interactive | autonomous | hybrid
```

## Existing Source Material

Inputs available for artifact generation:

| Source                                              | Useful for                                  |
| --------------------------------------------------- | ------------------------------------------- |
| `./docs/non_functional_requirements.md`             | URS (non-functional requirements)           |
| `./docs/contracts/contracts.md`                     | OpenAPI specs, container architecture       |
| `./docs/contracts/mongodb.md`                       | Domain concepts, data model                 |
| `./docs/architecture/system architecture v3.drawio` | Solution architecture                       |
| `./docs/diagrams/*.drawio`                          | Solution architecture, deployment view      |
| `./infrastructure/kubernetes/`                      | Container architecture, deployment topology |
| `./infrastructure/docker-local/`                    | Container architecture, local dev view      |
