NAMESPACE   := habo-banking
DIR         := .
SECRETS_DIR := secrets

.PHONY: help install-keda apply-secrets deploy teardown status logs rabbitmq-ui keda-status

help: ## Show available commands
	$(info Available commands:)
	$(info   install-keda        Install KEDA into the cluster via Helm)
	$(info   apply-secrets       Apply secrets (copy *.example.yaml -> *.yaml and fill in values first))
	$(info   deploy              Deploy the full stack -- run apply-secrets first)
	$(info   teardown            Delete the habo-banking namespace and all its resources)
	$(info   status              Show pod status across the namespace)
	$(info   logs                Tail logs for a service -- usage: make logs service=service-ai)
	$(info   rabbitmq-ui         Port-forward RabbitMQ management UI -> http://localhost:15672)
	$(info   keda-status         Show KEDA ScaledObjects and their current replica counts)
	@echo ""

install-keda: ## Install KEDA into the cluster via Helm
	helm repo add kedacore https://kedacore.github.io/charts
	helm repo update
	helm install keda kedacore/keda --namespace keda --create-namespace

apply-secrets: ## Apply secrets (copy *.example.yaml → *.yaml and fill in values first)
	kubectl apply -f "$(SECRETS_DIR)/rabbitmq-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/postgresql-transaction-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/ai-secret.yaml"
	kubectl apply -f "$(SECRETS_DIR)/smtp-secret.yaml"

deploy: ## Deploy the full stack — run apply-secrets first
	kubectl apply -f "$(DIR)/namespace.yaml"
	kubectl apply -f "$(DIR)/configmaps/"
	kubectl apply -f "$(DIR)/rabbitmq/"
	kubectl apply -f "$(DIR)/postgresql-transaction/"
	@kubectl get crd scaledobjects.keda.sh > /dev/null 2>&1 || (echo "KEDA CRDs not found - run 'make install-keda' first" && exit 1)
	kubectl apply -f "$(DIR)/service-currency-exchange/"
	kubectl apply -f "$(DIR)/service-ai/"
	kubectl apply -f "$(DIR)/service-notification/"
	kubectl apply -f "$(DIR)/service-transaction/"
	kubectl apply -f "$(DIR)/keda/"

teardown: ## Delete the habo-banking namespace and all its resources
	kubectl delete namespace $(NAMESPACE)

status: ## Show pod status across the namespace
	kubectl get pods -n $(NAMESPACE) -o wide

logs: ## Tail logs for a service — usage: make logs service=service-ai
	kubectl logs -f -l app="$(service)" -n $(NAMESPACE) --tail=100

rabbitmq-ui: ## Port-forward RabbitMQ management UI → http://localhost:15672
	kubectl port-forward svc/rabbitmq 15672:15672 -n $(NAMESPACE)

keda-status: ## Show KEDA ScaledObjects and their current replica counts
	kubectl get scaledobjects,hpa -n $(NAMESPACE)
