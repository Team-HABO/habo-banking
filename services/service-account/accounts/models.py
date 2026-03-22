import uuid

from django.db import models  # type: ignore[import-untyped]


class AccountType(models.Model):
    """Model representing an account type."""

    name = models.CharField(max_length=255, unique=True)

    class Meta:
        db_table = "account_types"

    def __str__(self):
        return self.name


class Account(models.Model):
    """Model representing an account."""

    owner_id = models.CharField(max_length=255)
    account_guid = models.UUIDField(default=uuid.uuid4, unique=True, editable=False)

    class Meta:
        db_table = "accounts"

    def __str__(self):
        return f"Account({self.account_guid})"


class AccountDetail(models.Model):
    """Model representing account details."""

    account = models.ForeignKey(
        Account, on_delete=models.CASCADE, related_name="details"
    )
    name = models.CharField(max_length=255)
    account_type = models.ForeignKey(
        AccountType, on_delete=models.PROTECT, related_name="account_details"
    )
    is_frozen = models.BooleanField(default=False)
    timestamp = models.DateTimeField(auto_now_add=True)

    class Meta:
        db_table = "account_details"

    def __str__(self):
        return f"AccountDetail({self.name})"


class DeletedAccount(models.Model):
    """Model representing a deleted account record."""

    account = models.ForeignKey(
        Account, on_delete=models.CASCADE, related_name="deleted_records"
    )
    timestamp = models.DateTimeField(auto_now_add=True)

    class Meta:
        db_table = "deleted_accounts"

    def __str__(self):
        return f"DeletedAccount({self.pk})"
