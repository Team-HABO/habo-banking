NAMESPACE   := habo-banking
DIR         := .
SECRETS_DIR := secrets

.PHONY: help install-keda apply-secrets deploy deploy-infra deploy-services deploy-observability \
        teardown status logs keda-status

help: ## Show available commands
	$(info Available commands:)
	$(info   install-keda          Install KEDA into the cluster via Helm)
	$(info   apply-secrets         Apply secrets (copy *.example.yaml -> *.yaml and fill in values first))
	$(info   deploy                Deploy the full stack -- run apply-secrets first)
	$(info   deploy-infra          Deploy infrastructure only (databases, message broker))
	$(info   deploy-services       Deploy application services only)
	$(info   deploy-observability  Deploy observability stack only (Prometheus, Loki, Alloy, Grafana))
	$(info   teardown              Delete the habo-banking namespace and all its resources)
	$(info   status                Show pod status across the namespace)
	$(info   logs                  Tail logs for a service -- usage: make logs service=service-ai)
	$(info   keda-status           Show KEDA ScaledObjects and their current replica counts)
	@echo ""

install-keda: ## Install KEDA into the cluster via Helm
	helm repo add kedacore https://kedacore.github.io/charts
	helm repo update
	helm install keda kedacore/keda --namespace keda --create-namespace

apply-secrets: ## Apply secrets (copy *.example.yaml → *.yaml and fill in values first)
	kubectl apply -f "$(SECRETS_DIR)/rabbitmq-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/postgresql-transaction-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/postgresql-account-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/ai-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/smtp-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/auth-secret.yaml"

deploy-infra: ## Deploy infrastructure only (databases, message broker)
	kubectl apply -f "$(DIR)/namespace.yaml"
	kubectl apply -f "$(DIR)/configmaps/"
	kubectl apply -f "$(DIR)/rabbitmq/"
	kubectl apply -f "$(DIR)/postgresql-transaction/"
	kubectl apply -f "$(DIR)/postgresql-account/"
	kubectl apply -f "$(DIR)/mongodb/"

deploy-services: ## Deploy application services only (run deploy-infra first)
	@kubectl get crd scaledobjects.keda.sh > /dev/null 2>&1 || (echo "KEDA CRDs not found - run 'make install-keda' first" && exit 1)
	kubectl apply -f "$(DIR)/service-currency-exchange/"
	kubectl apply -f "$(DIR)/service-ai/"
	kubectl apply -f "$(DIR)/service-notification/"
	kubectl apply -f "$(DIR)/service-transaction/"
	kubectl apply -f "$(DIR)/service-account/"
	kubectl apply -f "$(DIR)/service-account-consumer/"
	kubectl apply -f "$(DIR)/service-synchronize/"
	kubectl apply -f "$(DIR)/service-view/"
	kubectl apply -f "$(DIR)/service-auth/"
	kubectl apply -f "$(DIR)/service-frontend/"
	kubectl apply -f "$(DIR)/keda/"

deploy-observability: ## Deploy observability stack only (Prometheus, Loki, Alloy, Grafana)
	kubectl apply -f "$(DIR)/observability/alloy/serviceaccount.yaml"
	kubectl apply -f "$(DIR)/observability/alloy/rbac.yaml"
	kubectl apply -f "$(DIR)/observability/loki/"
	kubectl apply -f "$(DIR)/observability/alloy/"
	kubectl apply -f "$(DIR)/observability/prometheus/"
	kubectl apply -f "$(DIR)/observability/grafana/"

deploy: ## Deploy the full stack — run apply-secrets first
	$(MAKE) deploy-infra
	$(MAKE) deploy-services
	$(MAKE) deploy-observability

teardown: ## Delete the habo-banking namespace and all its resources
	kubectl delete namespace $(NAMESPACE)
	kubectl delete clusterrole alloy-log-reader --ignore-not-found
	kubectl delete clusterrolebinding alloy-log-reader --ignore-not-found

status: ## Show pod status across the namespace
	kubectl get pods -n $(NAMESPACE) -o wide

logs: ## Tail logs for a service — usage: make logs service=service-ai
	kubectl logs -f -l app="$(service)" -n $(NAMESPACE) --tail=100

keda-status: ## Show KEDA ScaledObjects and their current replica counts
	kubectl get scaledobjects,hpa -n $(NAMESPACE)
