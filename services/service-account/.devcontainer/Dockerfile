FROM python:3.12-slim

RUN apt-get update && apt-get install -y --no-install-recommends libpq-dev gcc \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY requirements.txt* /tmp/pip-tmp/
RUN if [ -f "/tmp/pip-tmp/requirements.txt" ]; then pip install --no-cache-dir -r /tmp/pip-tmp/requirements.txt; fi \
    && rm -rf /tmp/pip-tmp
