# Non-Functional Requirements
## Banking Application - Currency Exchange System

---

## 1. Performance
- Bank transaction operations must complete within 5 seconds
- Database queries must be optimized (under 200ms for most queries)

## 2. Scalability
- All microservices must be stateless
- Individual microservices must scale independently
- Kubernetes autoscaling must be configured

## 3. Reliability
- Financial transactions must follow ACID properties
- Implement Saga pattern for distributed transactions with compensating actions
- Idempotent message handlers with at-least-once delivery
- Dead letter queues for failed messages

## 4. Security
- JWT authentication with OAuth 2.0 (e.g., Google login)
- Role-based access control against relational databases (RBAC)
- Encrypt sensitive data at rest (AES-256)
- Secure storage for secrets (e.g. Kubernetes secrets, GitHub Secret Variables, not in git)
- Immutable audit logs for all bank transactions
- Protection against common attacks (SQL injection, XSS, CSRF)

## 5. Maintainability
- Static analysis in CI/CD pipeline
- Independent deployment per microservice
- API versioning for backward compatibility
- Database migration tools
- Swagger/OpenAPI for REST, schema docs for GraphQL, AsyncAPI for messaging

## 6. Portability
- All services containerized
- Externalized configuration (environment variables)
- Works in both docker-compose and Kubernetes

## 7. Interoperability
- Expose both REST and GraphQL APIs
- JSON and/or XML data format
- Standardized API contracts for external services (currency rates, AI)

## 8. Observability
- Structured logging with correlation IDs
- Health check endpoints on all services
- Centralized logs and metrics

## 9. AI Integration
- At least one AI service integrated (e.g., fraud detection, categorization)
- AI failures must not break core banking functions

## 10. Testing
- 80% code coverage with automated tests
- Integration tests for APIs and messaging
- System-level tests for multi-service interaction
- Security tests for auth/authorization
- All tests automated in CI/CD pipeline

## 11. Development Environment
- Runnable via docker-compose
- Deployable to local Kubernetes (Minikube/Kind)
- KEDA ScaledJob for serverless simulation
- CI/CD with automated builds and tests
