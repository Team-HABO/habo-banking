```json
{
    "user": {
        "id": "number",
        "accounts": [
            {
                "accountGuid": "string",
                "type": "string",
                "name": "string",
                "isFrozen": "boolean",
                "timestamp": "string",
                "balance": {
                    "amount": "string"
                },
                "audits": [
                    {
                        "auditId": "string",
                        "amount": "200",
                        "type": "string",
                        "timestamp": "string",
                        "receiver": "account name or null",
                        "sender": "account name or null"
                    }
                ]
            }
        ]
    }
}
```

