from django.db import models # type: ignore

class Account(models.Model):
    account_id = models.CharField(max_length=255, unique=True)
    created_at = models.DateTimeField(auto_now_add=True)
    updated_at = models.DateTimeField(auto_now=True)

    class Meta:
        db_table = 'accounts'

    def __str__(self):
        return f"Account({self.account_id})"