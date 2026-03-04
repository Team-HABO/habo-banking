# Contracts

| Id | Method | Description                          | Endpoint                      | Notes                                    |
|----|--------|--------------------------------------|-------------------------------|------------------------------------------|
| 1  | POST   | Create Account                       | /accounts                     |                                          |
| 2  | PATCH  | Freeze/Unfreeze Account              | /accounts/{guid}              |                                          |
| 3  | PUT    | Rename Account / Change Account Type | /accounts/{guid}              |                                          |
| 4  | DELETE | Delete Account                       | /accounts/{guid}              | Creates the account in the deleted table |
| 5  | POST   | Bank Transaction                     | /accounts/{guid}/transactions | Transfer, deposit and withdraw           |
| 6  | POST   | Currency Exchange                    | /accounts/{guid}/exchanges    | Exchange currency                        |

IMPORTANT: the `ownerId` is filled by the value inside the JWT!

## ID 1

Step 1, Initial POST request:

```json
{
    "guid": "string",
    "name": "string",
    "type": "string"
}
```

Step 2, Produce message to Transaction-Service:

```json
{
    "data": {
        "accountGuid": "string",
        "ownerId": "string",
        "type": "string",
        "name": "string",
        "isFrozen": "boolean",
        "timestamp": "string",
    },
    "metadata": {
        "messageType": "ACCOUNT_CREATE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 3, Produce message to Synchronize-Service:

```json
{
    "data": {
        "account": {
            "accountGuid": "string",
            "type": "string",
            "name": "string",
            "isFrozen": "boolean",
            "timestamp": "string",
            "balance": {
                "amount": "string",
                "timestamp": "string"
            }
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_CREATE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

## ID 2

Step 1, Initial PATCH request:

```json
{
    "freeze": "boolean (true/false)"
}
```

Step 2, Produce message to Synchronize-Service:

```json
{
    "data": {
        "account": {
            "accountGuid": "string",
            "isFrozen": "boolean",
            "timestamp": "string",
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_STATUS",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

## ID 3

Step 1, Initial PUT request:

```json
{
    "name": "string",
    "type": "string"
}
```

Step 2, Produce message to Synchronize-Service:

```json
{
    "data": {
        "account": {
            "accountGuid": "string",
            "name": "string",
            "type": "string",
            "timestamp": "string",
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_UPDATE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

## ID 4

Step 1, Initial DELETE request. No request.body.

Step 2, Produce message to Transaction-Service:

```json
{
    "data": {
        "accountGuid": "string",
        "ownerId": "string",
        "timestamp": "string",
    },
    "metadata": {
        "messageType": "ACCOUNT_DELETE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 3, Produce message to Synchronize-Service:

```json
{
    "data": {
        "account": {
            "accountGuid": "string",
            "timestamp": "string",
        }
    },
    "metadata": {
        "messageType": "ACCOUNT_DELETE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

## ID 5

Step 1, Initial POST request:

```json
{
    "receiverAccountGuid": "string (optional)",
    "amount": "string",
    "transactionType": "string"
}
```

Step 2, Produce message to Fraud-Service:

IMPORTANT: `data.receiver` object is only relevant if transaction type is transfer.

```json
{
    "data": {
        "receiver": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "transactionType": "string",
        "originIpAddress": "string",
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER/WITHDRAW/DEPOSIT",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 2.5, If fraudulent, Produce message to Notification-Service:

```json
{
    "data": {
        "message": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER/WITHDRAW/DEPOSIT",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 3, If NOT fraudulent, Produce message to Transaction-Service

IMPORTANT: `data.receiver` object is only relevant if transaction type is transfer.

```json
{
    "data": {
        "receiver": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "transactionType": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER/WITHDRAW/DEPOSIT",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 3.5, If not possible to do transaction type, then produce message to Notification-Service

```json
{
    "data": {
        "message": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER/WITHDRAW/DEPOSIT",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 4, Produce message to Synchronize-Service

```json
{
    "data": {
        "account": {
            "balance": {
                "amount": "string",
                "timestamp": "string"
            },
            "audits": {
                "receiver": "string (name, optional)",
                "amount": "string",
                "type": "string",
                "timestamp": "string"
            }
        }
    },
    "metadata": {
        "messageType": "TRANSACTION_TRANSFER/WITHDRAW/DEPOSIT",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

## ID 6

Step 1, Initial POST request:

```json
{
    "amount": "string",
    "currency": "string",
}
```

Step 2, Produce message to Fraud-Service:

```json
{
    "data": {
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "currency": "string",
        "transactionType": "exchange",
        "originIpAddress": "string",
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 2.5, If fraudulent, Produce message to Notification-Service:

```json
{
    "data": {
        "message": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 3, If NOT fraudulent, Produce message to Transaction-Service

```json
{
    "data": {
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "currency": "string",
        "transactionType": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 4, Produce message to Currency-Service

```json
{
    "data": {
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "currency": "string",
        "transactionType": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 5, Produce message back to Transaction-Service

```json
{
    "data": {
        "account": {
            "guid": "string",
            "name": "string",
            "type": "string",
        },
        "amount": "string",
        "currency": "string",
        "transactionType": "string",
        "exchangeRate": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 5.5, If not possible to do currency exchange, then produce message to Notification-Service

```json
{
    "data": {
        "message": "string"
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```

Step 6, Produce message to Synchronize-Service

```json
{
    "data": {
        "account": {
            "balance": {
                "amount": "string",
                "timestamp": "string"
            },
            "audits": {
                "amount": "string",
                "type": "exchange",
                "timestamp": "string"
            }
        }
    },
    "metadata": {
        "messageType": "TRANSACTION_EXCHANGE",
        "messageTimestamp": "dateTime.now()",
        "...": "..."
    }
}
```
