# service-view

GraphQL service for reading user account and audit data from MongoDB.

## Prerequisites

- Node.js 24+
- npm
- Docker Desktop (for MongoDB and Docker Compose flow)

## Environment

This service uses these environment variables:

- `MONGODB_CONNECTION_STRING`
- `SERVER_HOST` (default: `0.0.0.0`)
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
- `service-view` on `0.0.0.0:${SERVER_PORT:-4000}`

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

### Test in Apollo Server client on http://127.0.0.1:4000/
query GetAccountAudits($userId: ID!, $accountGuid: ID!) {
  getAccountAudits(userId: $userId, accountGuid: $accountGuid) {
    amount
    auditId
    receiver
    sender
    timestamp
    type
  }
}
Example variables:
{  
  "userId": "user-1",
  "accountGuid": "1"
}

query GetUserAccounts($userId: ID!) {
  getUserAccounts(userId: $userId) {
    accountGuid
    balance
    name
    type
  }
}
Example variables:
{  
  "userId": "user-1"
}
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
npm test -- tests/resolvers.test.ts
```

Run tests in watch mode during development:

```bash
npx vitest --watch
```