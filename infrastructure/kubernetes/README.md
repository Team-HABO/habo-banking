# Kubernetes — HABO Banking

Deployment guide for the full habo-banking microservices stack on Kubernetes.

---

## Architecture

The stack consists of infrastructure, application services, and an observability layer.

### Infrastructure

| Component              | Type         | Port  |
|------------------------|--------------|-------|
| RabbitMQ               | LoadBalancer | 15672 (management UI) / 5672 (AMQP) |
| PostgreSQL (transaction) | ClusterIP  | 5432  |
| PostgreSQL (account)   | ClusterIP    | 5432  |
| MongoDB (replica set)  | ClusterIP    | 27017 |

### Application services

| Service                  | Language     | Port | Description                              |
|--------------------------|--------------|------|------------------------------------------|
| service-transaction      | C#/.NET      | —    | Processes transactions (KEDA worker)     |
| service-currency-exchange | C#/.NET     | —    | Currency conversion (KEDA worker)        |
| service-ai               | C#/.NET      | —    | Fraud detection (KEDA worker)            |
| service-notification     | C#/.NET      | —    | Sends emails (KEDA worker)               |
| service-account          | Python/Django | 8000 | Account management HTTP API              |
| service-account-consumer | Python/Django | —    | Account event consumer (worker)          |
| service-auth             | C#/.NET      | 8080 | Google OAuth2 + JWT issuance             |
| service-synchronize      | C#/.NET      | —    | Syncs MongoDB from RabbitMQ events       |
| service-view             | TypeScript   | 4000 | Read-model queries via MongoDB           |
| service-frontend         | React SPA    | 3000 | Web UI                                   |

### Observability

| Component       | Port | Description                              |
|-----------------|------|------------------------------------------|
| Prometheus      | 9090 | Metrics scraping (ai, notification, currency-exchange) |
| Loki            | ClusterIP | Log aggregation                     |
| Grafana Alloy   | —    | Log collector (DaemonSet, K8s pod discovery) |
| Grafana         | 13000 | Dashboards — `admin/admin`             |

### Message flow

| Event                     | Published by              | Consumed by               | Queue                              |
|---------------------------|---------------------------|---------------------------|------------------------------------|
| Currency exchange request | service-transaction       | service-currency-exchange | `currency-exchange-requests-queue` |
| Exchange result           | service-currency-exchange | service-transaction       | `currency-exchange-response-queue` |
| Fraud check               | service-transaction       | service-ai                | `ai-transaction-queue`             |
| Fraud check result        | service-ai                | service-transaction       | `check-fraud`                      |
| Notification (exchange)   | service-currency-exchange | service-notification      | `notification-queue`               |
| Notification (fraud)      | service-ai                | service-notification      | `notification-queue`               |

---

## File structure

```
infrastructure/kubernetes/
├── Makefile                          ← All commands live here
├── README.md                         ← This file
├── namespace.yaml
├── configmaps/
│   └── app-config.yaml               ← Non-sensitive env vars (hosts, ports, URLs)
├── secrets/
│   ├── .gitignore                    ← Ignores *-secret.yaml (actual secrets)
│   ├── rabbitmq-secret.example.yaml
│   ├── postgresql-transaction-secret.example.yaml
│   ├── postgresql-account-secret.example.yaml
│   ├── ai-secret.example.yaml
│   ├── smtp-secret.example.yaml
│   └── auth-secret.example.yaml      ← Google OAuth + JWT secret
├── rabbitmq/
│   ├── deployment.yaml
│   ├── service.yaml                  ← LoadBalancer :15672 (management UI) / 5672 (AMQP)
│   └── pvc.yaml
├── postgresql-transaction/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP :5432
│   └── pvc.yaml
├── postgresql-account/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP :5432
│   └── pvc.yaml
├── mongodb/
│   ├── deployment.yaml               ← mongo:7 with rs0 replica set sidecar init
│   ├── service.yaml                  ← ClusterIP :27017
│   └── pvc.yaml
├── service-currency-exchange/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP :9093 (Prometheus scrape target)
│   └── scaledobject.yaml
├── service-ai/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP :9091 (Prometheus scrape target)
│   └── scaledobject.yaml
├── service-notification/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP :9092 (Prometheus scrape target)
│   └── scaledobject.yaml
├── service-transaction/
│   ├── deployment.yaml
│   └── scaledobject.yaml
├── service-account/
│   ├── deployment.yaml               ← Runs migrations + Django server
│   └── service.yaml                  ← LoadBalancer :8000
├── service-account-consumer/
│   └── deployment.yaml               ← Runs `python manage.py consume_events`
├── service-auth/
│   ├── deployment.yaml               ← Google OAuth2, issues JWTs
│   └── service.yaml                  ← LoadBalancer :8080
├── service-synchronize/
│   └── deployment.yaml               ← RabbitMQ → MongoDB sync worker
├── service-view/
│   ├── deployment.yaml               ← Reads from MongoDB change streams
│   └── service.yaml                  ← LoadBalancer :4000
├── service-frontend/
│   ├── deployment.yaml
│   └── service.yaml                  ← LoadBalancer :3000
├── observability/
│   ├── prometheus/
│   │   ├── configmap.yaml            ← Static scrape config (K8s DNS targets)
│   │   ├── deployment.yaml
│   │   ├── service.yaml              ← LoadBalancer :9090
│   │   └── pvc.yaml
│   ├── loki/
│   │   ├── configmap.yaml
│   │   ├── deployment.yaml
│   │   ├── service.yaml              ← ClusterIP :3100 (internal only)
│   │   └── pvc.yaml
│   ├── alloy/
│   │   ├── configmap.yaml            ← K8s pod discovery + log forwarding to Loki
│   │   ├── serviceaccount.yaml
│   │   ├── rbac.yaml                 ← ClusterRole for pod/log read access
│   │   └── deployment.yaml
│   └── grafana/
│       ├── configmap.yaml            ← Datasources + dashboard provisioning
│       ├── deployment.yaml
│       ├── service.yaml              ← LoadBalancer :13000
│       └── pvc.yaml
└── keda/
    └── rabbitmq-trigger-auth.yaml    ← TriggerAuthentication (management API URL)
```

---

## Prerequisites

| Tool                  | Purpose                    | Install                                                            |
|-----------------------|----------------------------|--------------------------------------------------------------------|
| `kubectl`             | Kubernetes CLI             | [docs.k8s.io/tasks/tools](https://kubernetes.io/docs/tasks/tools/) |
| Kind / Docker Desktop | Local cluster              | [kind.sigs.k8s.io](https://kind.sigs.k8s.io/)                      |
| `helm`                | Package manager (for KEDA) | [helm.sh/docs/intro/install](https://helm.sh/docs/intro/install/)  |

---

## First-time setup

### 1. Install Helm

KEDA is installed via Helm, so Helm must be on your PATH first.

**Windows (winget):**

```powershell
winget install Helm.Helm
```

**Windows (Chocolatey):**

```powershell
choco install kubernetes-helm
```

**macOS:**

```bash
brew install helm
```

**Linux:**

```bash
curl https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash
```

Restart your terminal after installing so `helm` is available on the PATH.

### 2. Install KEDA

```bash
make install-keda
```

Installs KEDA into its own `keda` namespace via Helm. This registers the `ScaledObject` and `TriggerAuthentication` CRDs that the `keda/` manifests depend on.

### 3. Create secret files

Each `secrets/*.example.yaml` is a template. Copy it, remove `.example` from the filename, and fill in real values:

```bash
cd secrets/

cp rabbitmq-secret.example.yaml              rabbitmq-secret.yaml
cp postgresql-transaction-secret.example.yaml postgresql-transaction-secret.yaml
cp postgresql-account-secret.example.yaml    postgresql-account-secret.yaml
cp ai-secret.example.yaml                    ai-secret.yaml
cp smtp-secret.example.yaml                  smtp-secret.yaml
cp auth-secret.example.yaml                  auth-secret.yaml

# Edit each file with your actual credentials
```

> **The `*-secret.yaml` files are gitignored** — they will never be committed. Only the `.example.yaml` templates are version controlled.

#### auth-secret fields

| Key                    | Description                                                              |
|------------------------|--------------------------------------------------------------------------|
| `GOOGLE_CLIENT_ID`     | OAuth2 client ID from Google Cloud Console                               |
| `GOOGLE_CLIENT_SECRET` | OAuth2 client secret from Google Cloud Console                           |
| `SECRET_KEY_JWT`       | Symmetric JWT signing key — **minimum 32 characters**. Shared between `service-auth` (signs tokens) and `service-view` (verifies tokens). |

### 4. Pull Docker images locally

If your cluster node cannot reach Docker Hub at pull time (e.g. network restrictions or EOF errors), pull all images manually first so they are cached in the local Docker daemon:

```bash
docker pull rabbitmq:3-management
docker pull postgres:15-alpine
docker pull mongo:7
docker pull busybox:1.37
docker pull prom/prometheus:latest
docker pull grafana/loki:latest
docker pull grafana/alloy:latest
docker pull grafana/grafana:latest
docker pull alihmohammad/habo-bank-service-transaction:latest
docker pull alihmohammad/habo-bank-service-frontend:latest
docker pull forkeh/habo-bank-service-currency-exchange:latest
docker pull forkeh/habo-bank-service-notification:latest
docker pull forkeh/habo-bank-service-ai:latest
docker pull han9salman/habo-bank-service-account:latest
docker pull olli4/habo-bank-service-auth:latest
docker pull olli4/habo-bank-service-synchronize:latest
docker pull olli4/habo-bank-service-view:latest
```

On Docker Desktop, the cluster shares the host's Docker image cache, so images pulled this way are available to Kubernetes without a registry pull.

### 5. Apply secrets and deploy

```bash
# Apply the namespace first, then secrets
kubectl apply -f namespace.yaml
make apply-secrets

# Deploy everything
make deploy
```

`make deploy` runs three sub-targets in order: `deploy-infra` → `deploy-services` → `deploy-observability`.

---

## Accessing services

### Application UIs

| Service         | URL                      | Credentials                         |
|-----------------|--------------------------|--------------------------------------|
| Frontend        | http://localhost:3000    | Google login (via service-auth)      |
| service-account | http://localhost:8000    | Internal API                         |
| service-auth    | http://localhost:8080    | —                                    |
| service-view    | http://localhost:4000    | JWT required                         |

### Observability UIs

| Service    | URL                       | Credentials  |
|------------|---------------------------|--------------|
| Grafana    | http://localhost:13000    | admin/admin  |
| Prometheus | http://localhost:9090     | —            |
| RabbitMQ   | http://localhost:15672    | —            |

All services use `LoadBalancer` type, so Docker Desktop maps them directly to `localhost` with no port-forwarding required.

### Exec into a service pod

```bash
# Find the pod name
kubectl get pods -n habo-banking

# Open a shell (use /bin/sh for .NET images, bash may not be available)
kubectl exec -it <pod-name> -n habo-banking -- /bin/sh
```

Useful things to do from inside a pod:

```sh
# Check environment variables (confirms secrets/configmap are mounted correctly)
env | grep RABBITMQ

# Test connectivity to RabbitMQ
nc -zv rabbitmq 5672

# Test connectivity to MongoDB
nc -zv mongodb 27017
```

To tail live logs without exec-ing in:

```bash
# Single pod
kubectl logs -f <pod-name> -n habo-banking

# All pods for a service (useful when KEDA has scaled to >1)
make logs service=service-ai
```

---

## Makefile commands

| Command                          | Description                                                      |
|----------------------------------|------------------------------------------------------------------|
| `make help`                      | Show all available commands                                      |
| `make install-keda`              | Install KEDA into the cluster via Helm                           |
| `make apply-secrets`             | Apply all secret files from `secrets/`                           |
| `make deploy`                    | Deploy the full stack (infra + services + observability)         |
| `make deploy-infra`              | Deploy infrastructure only (databases, RabbitMQ)                 |
| `make deploy-services`           | Deploy application services only (requires KEDA)                 |
| `make deploy-observability`      | Deploy Prometheus, Loki, Alloy, Grafana                          |
| `make teardown`                  | Delete the namespace and cluster-scoped Alloy RBAC resources     |
| `make status`                    | Show pod status with node assignment                             |
| `make logs service=<name>`       | Tail logs for a service (e.g. `make logs service=service-ai`)    |
| `make keda-status`               | Show KEDA ScaledObjects and current HPA replica counts           |

---

## KEDA — Kubernetes Event-Driven Autoscaling

KEDA extends Kubernetes with event-driven autoscaling. Instead of scaling on CPU/memory, it scales pods based on **external event sources** — in this project, RabbitMQ queue depth.

### How it works

1. KEDA polls the RabbitMQ **management HTTP API** every `pollingInterval` seconds
2. It reads the message count for the target queue
3. It calculates the desired replica count: `ceil(queueLength / value)`
4. Kubernetes adjusts the Deployment replicas accordingly
5. After the queue empties, it waits `cooldownPeriod` seconds before scaling down

### Scaling parameters (per service)

| Parameter         | Value | Meaning                                           |
|-------------------|-------|---------------------------------------------------|
| `minReplicaCount` | 1     | Always keep at least 1 pod running                |
| `maxReplicaCount` | 5     | Never exceed 5 pods                               |
| `pollingInterval` | 15s   | Check queue depth every 15 seconds                |
| `cooldownPeriod`  | 60s   | Wait 60 seconds before scaling down               |
| `value`           | 5     | Scale up when > 5 messages per replica are queued |

### Why `protocol: http` instead of AMQP?

The KEDA RabbitMQ scaler supports two protocols:

- `amqp` — connects as a consumer; can only monitor a single queue and uses broker resources
- `http` — queries the management REST API; can monitor any queue without consuming messages ✓

---

## Observability

### Prometheus

Scrapes metrics from the three C# worker services via dedicated ClusterIP services:

| Job                     | Target                                              |
|-------------------------|-----------------------------------------------------|
| `service-ai`            | `service-ai-metrics.habo-banking.svc.cluster.local:9091` |
| `service-notification`  | `service-notification-metrics.habo-banking.svc.cluster.local:9092` |
| `service-currency-exchange` | `service-currency-exchange-metrics.habo-banking.svc.cluster.local:9093` |

### Grafana Alloy

Alloy runs as a Deployment (one replica) and collects logs from all pods in the `habo-banking` namespace using Kubernetes pod discovery (`discovery.kubernetes`), then forwards them to Loki. It requires a `ClusterRole` with read access to pods and pod logs — this is created by `observability/alloy/rbac.yaml`.

> Note: the `teardown` target deletes the `alloy-log-reader` ClusterRole and ClusterRoleBinding explicitly, since these are cluster-scoped resources and are not removed by `kubectl delete namespace`.

### Grafana

Pre-provisioned with:
- **Prometheus** datasource pointing to `http://prometheus.habo-banking.svc.cluster.local:9090`
- **Loki** datasource pointing to `http://loki.habo-banking.svc.cluster.local:3100`
- **Services dashboard** with metrics panels and log panels for each worker service

Default credentials: `admin` / `admin`.

---

## Kubernetes resource concepts used

| Resource                  | Purpose                                                                    |
|---------------------------|----------------------------------------------------------------------------|
| `Namespace`               | Logical isolation — all habo-banking resources live in `habo-banking`      |
| `Deployment`              | Declares the desired state for a set of pods (image, replicas, env)        |
| `Service`                 | Stable DNS name and load balancer in front of a Deployment                 |
| `ConfigMap`               | Non-sensitive key-value config injected as env vars                        |
| `Secret`                  | Base64-encoded sensitive values (passwords, API keys) injected as env vars |
| `PersistentVolumeClaim`   | Request for durable storage that survives pod restarts                     |
| `ServiceAccount`          | Identity for a pod, used to grant it API access (Alloy)                    |
| `ClusterRole`             | Cluster-wide permission set (Alloy needs pod/log read access)              |
| `ClusterRoleBinding`      | Binds a ClusterRole to a ServiceAccount                                    |
| `ScaledObject`            | KEDA resource that links a Deployment to a scaling trigger                 |
| `TriggerAuthentication`   | KEDA resource that securely provides credentials to a scaler               |

### Service types

| Type        | Accessible from                         | Use case                                            |
|-------------|-----------------------------------------|-----------------------------------------------------|
| `ClusterIP` | Inside cluster only                     | Databases, Loki, internal metrics endpoints         |
| `NodePort`  | Host machine via `localhost:<nodePort>` | Application services and observability UIs          |

### Init containers

Services use `initContainers` to wait for their dependencies before the main container starts. This avoids bundling wait scripts inside images.

```yaml
initContainers:
  - name: wait-for-rabbitmq
    image: busybox:1.37
    command: ['sh', '-c', 'until nc -z rabbitmq 5672; do sleep 2; done']
```

### MongoDB replica set

`service-view` uses MongoDB change streams, which require a replica set. The MongoDB Deployment uses a sidecar container that waits for MongoDB to become ready, then calls `rs.initiate()` once. The replica set name is `rs0` and is reflected in the connection string in `configmaps/app-config.yaml`.

---

## kubectl cheatsheet

### Cluster state

```bash
# All pods in the namespace
kubectl get pods -n habo-banking

# Detailed pod info including node, IP, restarts
kubectl get pods -n habo-banking -o wide

# Watch pods update in real time
kubectl get pods -n habo-banking -w

# All resources in the namespace
kubectl get all -n habo-banking

# KEDA ScaledObjects and HPA (auto-generated by KEDA)
kubectl get scaledobjects,hpa -n habo-banking
```

### Debugging

```bash
# Describe a pod (events, init container status, resource limits)
kubectl describe pod <pod-name> -n habo-banking

# Describe a deployment
kubectl describe deployment service-ai -n habo-banking

# Tail logs for a deployment
kubectl logs -f deployment/service-ai -n habo-banking

# Tail logs across all pods of a service (useful when scaled to >1)
kubectl logs -f -l app=service-ai -n habo-banking --tail=100

# Previous container logs (if pod crashed and restarted)
kubectl logs <pod-name> -n habo-banking --previous

# Check init container logs
kubectl logs <pod-name> -c wait-for-rabbitmq -n habo-banking

# Get events for the namespace (good first place to look when pods won't start)
kubectl get events -n habo-banking --sort-by='.lastTimestamp'
```

### Exec into a running pod

```bash
# Open a shell inside a pod (useful for debugging connectivity)
kubectl exec -it <pod-name> -n habo-banking -- sh

# Test that RabbitMQ is reachable from inside a pod
kubectl exec -it <pod-name> -n habo-banking -- nc -zv rabbitmq 5672
```

### Port forwarding

```bash
# RabbitMQ management UI → http://localhost:15672
kubectl port-forward svc/rabbitmq 15672:15672 -n habo-banking

# Grafana → http://localhost:3030
kubectl port-forward svc/grafana 3030:3000 -n habo-banking

# Prometheus → http://localhost:9090
kubectl port-forward svc/prometheus 9090:9090 -n habo-banking

# PostgreSQL (account) → localhost:5433 (useful with a DB GUI like TablePlus)
kubectl port-forward svc/postgresql-account 5433:5432 -n habo-banking

# PostgreSQL (transaction) → localhost:5432
kubectl port-forward svc/postgresql-transaction 5432:5432 -n habo-banking
```

### Manual scaling (bypasses KEDA)

```bash
# Scale a deployment manually
kubectl scale deployment service-ai --replicas=3 -n habo-banking

# Note: KEDA will override this once the next polling interval fires.
# To permanently adjust scaling, change maxReplicaCount in the ScaledObject.
```

### Secrets and ConfigMaps

```bash
# List secrets (values are hidden)
kubectl get secrets -n habo-banking

# View decoded secret values
kubectl get secret rabbitmq-secret -n habo-banking -o jsonpath='{.data}' \
  | jq 'to_entries[] | {(.key): (.value | @base64d)}'

# View ConfigMap
kubectl get configmap app-config -n habo-banking -o yaml
```

### Applying changes

```bash
# Apply a single file
kubectl apply -f service-ai/deployment.yaml

# Apply all files in a directory
kubectl apply -f service-ai/

# Force restart a deployment (e.g. to pull a new :latest image)
kubectl rollout restart deployment/service-ai -n habo-banking

# Check rollout status
kubectl rollout status deployment/service-ai -n habo-banking
```

### Cleanup

```bash
# Delete a single resource
kubectl delete deployment service-ai -n habo-banking

# Delete everything (irreversible — deletes PVCs and cluster-scoped Alloy RBAC too)
make teardown
```
