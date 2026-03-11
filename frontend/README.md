# Frontend Service

This is the frontend for the Habo Banking project, built with:

- Vite
- React
- TypeScript

## Requirements

- Node.js 20+
- npm

## Run Locally

1. Install dependencies:

```bash
npm install
```

2. Set the API URL (PowerShell):

```powershell
$env:VITE_AUTH_API_URL = "http://localhost:8080"
```

3. Start the development server:

```bash
npm run dev
```

4. Open:

`http://localhost:3000`

## Build and Preview

```bash
npm run build
npm run preview
```


## Run with Docker Compose

From the `frontend` folder:

```bash
docker compose down
docker compose up -d --build
```

App URL:

`http://localhost:3000`

`docker-compose.yml` passes `AuthServiceUrl` into `VITE_AUTH_API_URL` during image build.

## Sending API requests with token

If the backend stores the JWT in an `HttpOnly` cookie, you do not read the token in JavaScript.
Instead, send requests with credentials so the browser includes the cookie automatically.

Example:

```ts
const res = await axios.get("http://localhost/api/getrequestwithtoken", {
  withCredentials: true,
});
```

Notes:

- `withCredentials: true` is required for cross-origin requests (for example `localhost:3000` to `localhost:8080`).
- The backend must allow credentials in CORS (`Access-Control-Allow-Credentials: true`).
- `Access-Control-Allow-Origin` must be a specific origin, not `*`.

## Run unit test with vitest

```bash
npm run test
```