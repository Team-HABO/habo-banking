# service-view

GraphQL service for reading user account and audit data from MongoDB.

## Prerequisites

- Node.js 24+
- npm
- Docker Desktop (for MongoDB and Docker Compose flow)

## Environment

This service uses these environment variables:

- `MONGODB_CONNECTION_STRING`
- `SERVER_HOST` (default: `localhost`)
- `SERVER_PORT` (default: `4000`)

You can copy `.env.example` to `.env` and update if needed.

## Run Locally (Node.js)

1. Install dependencies:

```bash
npm install
```

2. Start MongoDB with Docker Compose (includes replica set init required by the connection string):

```bash
docker compose up -d mongodb
```

3. Start the service in development mode:

```bash
npm run dev
```

4. Open GraphQL endpoint:

- `http://<SERVER_HOST>:<SERVER_PORT>/`

## Run With Docker Compose

From `services/service-view`:

```bash
docker compose up --build
```

This starts:

- `mongodb` on `localhost:27017`
- `service-view` on `localhost:${SERVER_PORT:-4000}`

To stop:

```bash
docker compose down
```

To stop and remove MongoDB volume:

```bash
docker compose down -v
```

## Example Requests

The service exposes two queries:

- `getUserAccounts(userId: ID!)`
- `getAccountAudits(userId: ID!, accountGuid: ID!)`

Use POST requests to `http://localhost:<SERVER_PORT>/`.

### 1) getUserAccounts

```bash
curl -X POST http://localhost:$SERVER_PORT/ \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query ($userId: ID!) { getUserAccounts(userId: $userId) { accountGuid type name balance audits { auditId amount type timestamp sender receiver } } }",
    "variables": {
      "userId": "user-123"
    }
  }'
```

PowerShell alternative:

```powershell
$body = @{
  query = 'query ($userId: ID!) { getUserAccounts(userId: $userId) { accountGuid type name balance audits { auditId amount type timestamp sender receiver } } }'
  variables = @{
    userId = 'user-123'
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://localhost:$env:SERVER_PORT/" -Method Post -ContentType 'application/json' -Body $body
```

### 2) getAccountAudits

```bash
curl -X POST http://localhost:$SERVER_PORT/ \
  -H "Content-Type: application/json" \
  -d '{
    "query": "query ($userId: ID!, $accountGuid: ID!) { getAccountAudits(userId: $userId, accountGuid: $accountGuid) { auditId amount type timestamp sender receiver } }",
    "variables": {
      "userId": "user-123",
      "accountGuid": "acc-001"
    }
  }'
```

PowerShell alternative:

```powershell
$body = @{
  query = 'query ($userId: ID!, $accountGuid: ID!) { getAccountAudits(userId: $userId, accountGuid: $accountGuid) { auditId amount type timestamp sender receiver } }'
  variables = @{
    userId = 'user-123'
    accountGuid = 'acc-001'
  }
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://localhost:$env:SERVER_PORT/" -Method Post -ContentType 'application/json' -Body $body
```

## Notes

- The resolver returns empty arrays when no matching user/account is found.
- `Account.balance` is stored as MongoDB `Decimal128` and exposed as a string.

## Tests

Unit tests use Vitest. From the `services/service-view` folder run:

```bash
npm ci
npm test
```

Run a single test file or pattern:

```bash
npm test -- tests/resolvers.test.ts -- --run
```

Run tests in watch mode during development:

```bash
npx vitest --watch
```