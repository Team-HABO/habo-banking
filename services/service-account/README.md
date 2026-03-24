# Service Account

Minimal Django microservice for account data, running in a VS Code Dev Container with PostgreSQL.

## Project Structure

- [`manage.py`](manage.py)
- [`account_service/settings.py`](account_service/settings.py)
- [`account_service/urls.py`](account_service/urls.py)
- [`accounts/models.py`](accounts/models.py)
- [`accounts/views.py`](accounts/views.py)
- [`accounts/migrations/0001_initial.py`](accounts/migrations/0001_initial.py)
- [`.devcontainer/docker-compose.yml`](.devcontainer/docker-compose.yml)

## Endpoints

Defined in [`account_service/urls.py`](account_service/urls.py) and implemented in [`accounts/views.py`](accounts/views.py):

- `GET /` → welcome message
- `GET /health/` → `{ "status": "ok" }`

## Tech Stack

- Django 5.0.1 (see [`requirements.txt`](requirements.txt))
- PostgreSQL 15 (see [`.devcontainer/docker-compose.yml`](.devcontainer/docker-compose.yml))
- Dev Container config (see [`.devcontainer/devcontainer.json`](.devcontainer/devcontainer.json))

## Run in Dev Container

1. Open project in VS Code.
2. Run **Dev Containers: Rebuild and Reopen in Container**.
3. Start server:
   ```bash
   python manage.py runserver 0.0.0.0:8000
   ```

## Database

Connection config is in [`DATABASES`](account_service/settings.py) inside [`account_service/settings.py`](account_service/settings.py).

### Apply only app migrations

```bash
python manage.py migrate accounts
```

### Open PostgreSQL shell

```bash
python manage.py dbshell
```

### List tables

```sql
\dt
```

Expected core tables from [`accounts/migrations/0001_initial.py`](accounts/migrations/0001_initial.py):

- `accounts`
- `account_types`
- `account_details`
- `deleted_accounts`
- `django_migrations` (Django migration tracking)

## Query Data

Example in `dbshell`:

```sql
SELECT * FROM accounts;
SELECT * FROM account_types;
SELECT * FROM account_details;
SELECT * FROM deleted_accounts;
```

## Local Demo Script

You can run [`demo.py`](demo.py):

```bash
python demo.py
```