# Service Auth

`service-auth` is an ASP.NET Web API microservice responsible for user authentication.

## Overview

This service allows users to sign in with their Google account using OAuth.

After a successful OAuth login:

1. The user is authenticated with Google.
2. The API generates a JWT (JSON Web Token).
3. The JWT is returned by the API in an HTTP-only cookie and can be used to authorize requests to protected services.

Read the `auth_token` cookie from incoming HTTP requests on the server side.
Validate the JWT (signature, issuer, audience, expiry, etc.) using the configured signing key.

## Environment variables

See .\services\service-auth\.env.example

## Network
Must run on the same network as the frontend and API gateway.
To inspect the network, use:
    docker network inspect "Network name"
Look for IPAM.Config.Subnet
That value must be set in `.env` for the `NetworkIp` variable.

## Docker Compose usage
From `./services/service-auth`:

- Production-like run (base compose only):
    - `docker compose -f docker-compose.yml up -d --build`
- Development run (with override):
    - `docker compose -f docker-compose.yml -f docker-compose.override.yml up -d --build`

Tip: `docker compose up -d --build` automatically includes `docker-compose.override.yml` when it exists.

## Endpoints
Health:
*/health
Login:
*/api/auth/login
Google OAuth callback:
*/api/auth/google-response
Logout:
*/api/auth/logout
