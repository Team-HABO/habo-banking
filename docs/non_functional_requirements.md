# Non-Functional Requirements
## Banking Application - Currency Exchange System

---

## 1. Performance
- Transaction operations must complete within 2 seconds
- Database queries must be optimized (under 200ms for most queries)

## 2. Scalability
- All microservices must be stateless
- Individual microservices must scale independently
- Kubernetes autoscaling must be configured

## 3. Reliability
- Financial transactions must follow ACID properties
- Implement Saga pattern for distributed transactions with compensating actions
- Idempotent message handlers with at-least-once delivery
- Circuit breakers to prevent cascading failures
- Dead letter queues for failed messages

## 4. Security
- JWT authentication with OAuth 2.0 (e.g., Google login)
- Role-based access control (RBAC)
- Encrypt sensitive data at rest (AES-256)
- Secure storage for secrets (Kubernetes secrets, not in git)
- Immutable audit logs for all transactions
- Protection against common attacks (SQL injection, XSS, CSRF)

## 5. Maintainability
- 80% code coverage with automated tests
- Static analysis in CI/CD pipeline
- Independent deployment per microservice
- API versioning for backward compatibility
- Database migration tools
- Swagger/OpenAPI for REST, schema docs for GraphQL, AsyncAPI for messaging

## 6. Portability
- All services containerized with Docker
- Externalized configuration (environment variables)
- Works in both docker-compose and Kubernetes

## 7. Interoperability
- Expose both REST and GraphQL APIs
- JSON data format
- Standardized API contracts for external services (currency rates, AI)

## 8. Observability
- Structured logging with correlation IDs
- Health check endpoints on all services
- Centralized logs and metrics in Kubernetes

## 9. AI Integration
- At least one AI service integrated (e.g., fraud detection, categorization)
- Use local containerized LLM (Ollama) to avoid costs
- AI failures must not break core banking functions

## 10. Testing
- 80% unit test coverage
- Integration tests for APIs and messaging
- System-level tests for multi-service interaction
- Security tests for auth/authorization
- All tests automated in CI/CD pipeline

## 11. Development Environment
- Runnable via docker-compose
- Deployable to local Kubernetes (Minikube/Kind)
- Resource limits defined for all services
- Kubernetes HPA for autoscaling
- KEDA ScaledJob for serverless simulation
- CI/CD with automated builds and tests
