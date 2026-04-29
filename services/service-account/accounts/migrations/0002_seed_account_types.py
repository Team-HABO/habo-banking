"""Seed default account types."""

from django.db import migrations  # type: ignore[import-untyped]


def seed_account_types(apps, schema_editor):
    """Create default account types."""
    account_type = apps.get_model("accounts", "AccountType")
    types = ["checking", "savings", "pension"]
    for name in types:
        account_type.objects.get_or_create(name=name)


def reverse_seed(apps, schema_editor):
    """Remove seeded account types."""
    account_type = apps.get_model("accounts", "AccountType")
    account_type.objects.filter(name__in=["checking", "savings", "pension"]).delete()


class Migration(migrations.Migration):
    """Seed account types migration."""

    dependencies = [
        ("accounts", "0001_initial"),
    ]

    operations = [
        migrations.RunPython(seed_account_types, reverse_seed),
    ]
