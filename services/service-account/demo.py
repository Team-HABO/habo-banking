import os
import django

os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'account_service.settings')
django.setup()

from django.test import Client

client = Client()

print("\n Account Service Demo\n")

# Test: Health Check
print("Health Check")
resp = client.get('/health/')
print(f"   Status: {resp.status_code}")
print(f"   Response: {resp.json()}\n")

# Test: Hello World
print("Hello World")
print("Message: Hello World!\n")