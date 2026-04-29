"""Request / response serializers for the account service.

Each serializer handles validation for one specific endpoint.
The response serializer resolves the *latest* immutable
AccountDetail row to present the current state of an account.
"""

from accounts.models import Account, AccountType
from rest_framework import serializers  # type: ignore[import-untyped]

# Request serializers (validation only – no .create() / .update())


class CreateAccountSerializer(serializers.Serializer):
    """POST /accounts – validate name + type."""

    name = serializers.CharField(max_length=255)
    type = serializers.CharField(max_length=255)

    def validate_type(self, value):
        """Ensure the referenced account type exists."""
        if not AccountType.objects.filter(name=value).exists():
            raise serializers.ValidationError(f"Account type '{value}' does not exist.")
        return value


class UpdateAccountSerializer(serializers.Serializer):
    """PUT /accounts/{guid} – rename / change type."""

    name = serializers.CharField(max_length=255)
    type = serializers.CharField(max_length=255)

    def validate_type(self, value):
        """Ensure the referenced account type exists."""
        if not AccountType.objects.filter(name=value).exists():
            raise serializers.ValidationError(f"Account type '{value}' does not exist.")
        return value


class FreezeAccountSerializer(serializers.Serializer):
    """PATCH /accounts/{guid} – freeze or unfreeze."""

    freeze = serializers.BooleanField()


class TransactionSerializer(serializers.Serializer):
    """POST /accounts/{guid}/transactions."""

    receiverAccountGuid = serializers.CharField(required=False, allow_blank=True)
    amount = serializers.CharField(max_length=50)
    transactionType = serializers.ChoiceField(
        choices=["TRANSFER", "WITHDRAW", "DEPOSIT"],
    )
    messageId = serializers.UUIDField()

    def validate(self, attrs):
        """TRANSFER requires a receiver GUID."""
        if attrs["transactionType"] == "TRANSFER" and not attrs.get(
            "receiverAccountGuid"
        ):
            raise serializers.ValidationError(
                "receiverAccountGuid is required for TRANSFER."
            )
        return attrs


class ExchangeSerializer(serializers.Serializer):
    """POST /accounts/{guid}/exchanges."""

    amount = serializers.CharField(max_length=50)
    currency = serializers.CharField(max_length=10)
    messageId = serializers.UUIDField()


# Response serializer


class AccountResponseSerializer(serializers.ModelSerializer):
    """Serialize an Account using its latest immutable AccountDetail."""

    name = serializers.SerializerMethodField()
    type = serializers.SerializerMethodField()
    is_frozen = serializers.SerializerMethodField()
    timestamp = serializers.SerializerMethodField()

    class Meta:
        model = Account
        fields = [
            "account_guid",
            "owner_id",
            "name",
            "type",
            "is_frozen",
            "timestamp",
        ]

    def _get_latest_detail(self, obj):
        return obj.details.order_by("-timestamp").first()

    def get_name(self, obj):
        detail = self._get_latest_detail(obj)
        return detail.name if detail else None

    def get_type(self, obj):
        detail = self._get_latest_detail(obj)
        return detail.account_type.name if detail else None

    def get_is_frozen(self, obj):
        detail = self._get_latest_detail(obj)
        return detail.is_frozen if detail else False

    def get_timestamp(self, obj):
        detail = self._get_latest_detail(obj)
        return detail.timestamp.isoformat() if detail else None
