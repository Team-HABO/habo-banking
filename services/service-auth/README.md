# Service Auth

`service-auth` is an ASP.NET Web API microservice responsible for user authentication.

## Overview

This service allows users to sign in with their Google account using OAuth.

After a successful OAuth login:

1. The user is authenticated with Google.
2. The API generates a JWT (JSON Web Token).
3. The JWT is returned by the API and can be used to authorize requests to protected services.

## Purpose

This microservice centralizes authentication so other services in the system can rely on JWT-based identity and authorization.

## Environment variables

See .\services\service-auth\.env.example

## Network
Must run on the same network as the frontend and API gateway.
To inspect the network, use:
    docker network inspect "Network name"
Look for IPAM.Config.Subnet
That value must be set in `.env` for the `NetworkIp` variable.

## Run Docker container
cd .\services\service-auth\
docker compose build
docker compose up -d

## Endpoints
Health:
*/health
Login:
*/api/auth/login
Google OAuth callback:
*/api/auth/google-response
Logout:
*/api/auth/logout
