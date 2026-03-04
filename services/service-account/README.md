# Service Account

A minimal Django microservice demo.

## Overview

- **Health Check**: `/health/` endpoint to verify the service is running
- **Welcome Endpoint**: `/` returns a welcome message

## Prerequisites

- Docker
- VSCode with [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) extension

## Getting Started

1. Open this folder in VSCode.
2. Run `SHIFT + CTRL + P` and select `>Dev Containers: Rebuild and Reopen in Container`.

## Usage

### Running the Development Server

```bash
python manage.py runserver 0.0.0.0:8000
```

### Health Check

Open [http://localhost:8000/health/](http://localhost:8000/health/) in your browser  
or run:
```bash
curl http://localhost:8000/health/
```

### Welcome Endpoint

Open [http://localhost:8000/](http://localhost:8000/) in your browser

### Database Migrations (if needed)

```bash
python manage.py makemigrations
python manage.py migrate
```