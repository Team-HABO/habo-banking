# Kubernetes — HABO Banking

General Kubernetes notes and deployment guide for the habo-banking microservices.

---

## Architecture

All four services are **background workers** — they have no HTTP endpoints and communicate exclusively through RabbitMQ message queues.

```
┌─────────────────────────────────────────────────────────────────┐
│                     Kubernetes Cluster                          │
│                   namespace: habo-banking                       │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │                      RabbitMQ                            │  │
│  │  Queues:                                                  │  │
│  │  • currency-exchange-requests-queue                       │  │
│  │  • currency-exchange-response-queue (→ transaction)       │  │
│  │  • ai-transaction-queue                                   │  │
│  │  • check-fraud (→ transaction)                            │  │
│  │  • notification-queue                                     │  │
│  └──────────┬──────────┬──────────┬──────────┬───────────┘  │
│             │          │          │          │               │
│             ▼          ▼          ▼          ▼               │
│  ┌──────────────┐ ┌────────┐ ┌──────────┐ ┌─────────────┐  │
│  │  currency-   │ │  ai    │ │notification│ │transaction  │  │
│  │  exchange    │ │        │ │            │ │             │  │
│  │  (.NET)      │ │ (.NET) │ │  (.NET)    │ │  (Node.js)  │  │
│  └──────────────┘ └────────┘ └──────────┘ └──────┬──────┘  │
│                                                    │         │
│                                                    ▼         │
│                                             ┌────────────┐   │
│                                             │ PostgreSQL │   │
│                                             └────────────┘   │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  KEDA  — scales each service based on queue depth       │  │
│  │  (polls RabbitMQ management API every 15 seconds)       │  │
│  └────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Message flow

| Event                     | Published by              | Consumed by               | Queue                              |
| ------------------------- | ------------------------- | ------------------------- | ---------------------------------- |
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
│   └── app-config.yaml               ← Non-sensitive env vars (host names, ports)
├── secrets/
│   ├── .gitignore                    ← Ignores *-secret.yaml (actual secrets)
│   ├── rabbitmq-secret.example.yaml  ← Template — copy and fill in
│   ├── postgres-secret.example.yaml
│   ├── ai-secret.example.yaml
│   └── smtp-secret.example.yaml
├── rabbitmq/
│   ├── deployment.yaml
│   ├── service.yaml                  ← NodePort 30008 for management UI
│   └── pvc.yaml                      ← 1Gi persistent storage
├── postgresql/
│   ├── deployment.yaml
│   ├── service.yaml                  ← ClusterIP (internal only)
│   └── pvc.yaml                      ← 2Gi persistent storage
├── service-currency-exchange/
│   └── deployment.yaml
├── service-ai/
│   └── deployment.yaml
├── service-notification/
│   └── deployment.yaml
├── service-transaction/
│   └── deployment.yaml
└── keda/
    ├── rabbitmq-trigger-auth.yaml    ← TriggerAuthentication (management API URL)
    ├── service-currency-exchange-scaledobject.yaml
    ├── service-ai-scaledobject.yaml
    ├── service-notification-scaledobject.yaml
    └── service-transaction-scaledobject.yaml
```

---

## Prerequisites

| Tool                  | Purpose                    | Install                                                            |
| --------------------- | -------------------------- | ------------------------------------------------------------------ |
| `kubectl`             | Kubernetes CLI             | [docs.k8s.io/tasks/tools](https://kubernetes.io/docs/tasks/tools/) |
| Kind / Docker Desktop | Local cluster              | [kind.sigs.k8s.io](https://kind.sigs.k8s.io/)                      |
| `helm`                | Package manager (for KEDA) | [helm.sh/docs/intro/install](https://helm.sh/docs/intro/install/)  |

---

## First-time setup

### 1. Install KEDA

```bash
make install-keda
```

Installs KEDA into its own `keda` namespace via Helm.

### 2. Replace DockerHub image placeholders

The deployment files contain placeholder image names. Replace them with the real DockerHub usernames:

```bash
# On Linux/macOS:
BRIAN=your_brian_username
ALI=your_ali_username

sed -i "s/BRIAN_DOCKERHUB_USERNAME/$BRIAN/g" \
  service-currency-exchange/deployment.yaml \
  service-ai/deployment.yaml \
  service-notification/deployment.yaml

sed -i "s/ALI_DOCKERHUB_USERNAME/$ALI/g" \
  service-transaction/deployment.yaml

# On Windows (PowerShell):
# (Get-Content service-ai\deployment.yaml) -replace 'BRIAN_DOCKERHUB_USERNAME','actual_username' | Set-Content service-ai\deployment.yaml
```

The image name pattern (from GitHub Actions) is: `<DOCKERHUB_USERNAME>/habo-bank-<service-name>:latest`

### 3. Create secret files

Each `secrets/*.example.yaml` is a template. Copy it, remove `.example` from the filename, and fill in real values:

```bash
cd secrets/

cp rabbitmq-secret.example.yaml  rabbitmq-secret.yaml
cp postgres-secret.example.yaml  postgres-secret.yaml
cp ai-secret.example.yaml        ai-secret.yaml
cp smtp-secret.example.yaml      smtp-secret.yaml

# Edit each file with your actual credentials
```

> **The `*-secret.yaml` files are gitignored** — they will never be committed. Only the `.example.yaml` templates are version controlled.

### 4. Apply secrets and deploy

```bash
# Apply the namespace first, then secrets
kubectl apply -f namespace.yaml
make apply-secrets

# Deploy everything else
make deploy
```

---

## Makefile commands

| Command                    | Description                                                     |
| -------------------------- | --------------------------------------------------------------- |
| `make help`                | Show all available commands                                     |
| `make install-keda`        | Install KEDA into the cluster via Helm                          |
| `make apply-secrets`       | Apply secret files from `secrets/`                              |
| `make deploy`              | Deploy all resources (namespace → infra → services → KEDA)      |
| `make teardown`            | Delete the `habo-banking` namespace and all its resources       |
| `make status`              | Show pod status with node assignment                            |
| `make logs service=<name>` | Tail logs for a service (e.g. `make logs service=service-ai`)   |
| `make rabbitmq-ui`         | Port-forward RabbitMQ management UI to `http://localhost:15672` |
| `make keda-status`         | Show KEDA ScaledObjects and current HPA replica counts          |

---

## KEDA — Kubernetes Event-Driven Autoscaling

KEDA extends Kubernetes with event-driven autoscaling. Instead of scaling on CPU/memory, it scales your pods based on **external event sources** — in this project, RabbitMQ queue depth.

### How it works

1. KEDA polls the RabbitMQ **management HTTP API** every `pollingInterval` seconds
2. It reads the message count for the target queue
3. It calculates the desired replica count: `ceil(queueLength / value)`
4. Kubernetes adjusts the Deployment replicas accordingly
5. After the queue empties, it waits `cooldownPeriod` seconds before scaling down

### Scaling parameters (per service)

| Parameter         | Value | Meaning                                           |
| ----------------- | ----- | ------------------------------------------------- |
| `minReplicaCount` | 1     | Always keep at least 1 pod running                |
| `maxReplicaCount` | 5     | Never exceed 5 pods                               |
| `pollingInterval` | 15s   | Check queue depth every 15 seconds                |
| `cooldownPeriod`  | 60s   | Wait 60 seconds before scaling down               |
| `value`           | 5     | Scale up when > 5 messages per replica are queued |

### Why `protocol: http` instead of AMQP?

The KEDA RabbitMQ scaler supports two protocols:

- `amqp` — connects as a consumer; can only monitor a single queue and uses broker resources
- `http` — queries the management REST API; can monitor any queue without consuming messages ✓

### Why no Prometheus exporter?

The `microservices-scaling` project uses a `rabbitmq-exporter` sidecar to expose queue metrics to Prometheus, and KEDA then reads from Prometheus. This works but adds an extra hop and an extra service to maintain. The `http` protocol in KEDA talks directly to RabbitMQ's built-in management API — no exporter needed.

---

## Kubernetes resource concepts used

| Resource                | Purpose                                                                    |
| ----------------------- | -------------------------------------------------------------------------- |
| `Namespace`             | Logical isolation — all habo-banking resources live in `habo-banking`      |
| `Deployment`            | Declares the desired state for a set of pods (image, replicas, env)        |
| `Service`               | Stable DNS name and load balancer in front of a Deployment                 |
| `ConfigMap`             | Non-sensitive key-value config injected as env vars                        |
| `Secret`                | Base64-encoded sensitive values (passwords, API keys) injected as env vars |
| `PersistentVolumeClaim` | Request for durable storage that survives pod restarts                     |
| `ScaledObject`          | KEDA resource that links a Deployment to a scaling trigger                 |
| `TriggerAuthentication` | KEDA resource that securely provides credentials to a scaler               |

### Service types

| Type        | Accessible from                         | Use case                                   |
| ----------- | --------------------------------------- | ------------------------------------------ |
| `ClusterIP` | Inside cluster only                     | Default; PostgreSQL, internal services     |
| `NodePort`  | Host machine via `localhost:<nodePort>` | RabbitMQ management UI for local debugging |

### Init containers

Services use `initContainers` to wait for their dependencies to become available before the main container starts. This replaces the `wait-for-it.sh` script approach used in `microservices-scaling` — no script needs to be bundled inside the image.

```yaml
initContainers:
    - name: wait-for-rabbitmq
      image: busybox:1.37
      command: ['sh', '-c', 'until nc -z rabbitmq 5672; do sleep 2; done']
```

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

# PostgreSQL → localhost:5432 (useful with a DB GUI like TablePlus)
kubectl port-forward svc/postgresql 5432:5432 -n habo-banking
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

# Delete everything in the namespace (irreversible — deletes PVCs too)
make teardown
# or:
kubectl delete namespace habo-banking
```
