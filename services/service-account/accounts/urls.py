"""URL routing for accounts app.

Maps HTTP endpoints to view functions following the contracts:
  POST   /accounts              → create a new account (Contract 1)
  PUT    /accounts/{guid}       → rename / change account type (Contract 3)
  PATCH  /accounts/{guid}       → freeze or unfreeze account (Contract 2)
  DELETE /accounts/{guid}       → soft-delete account (Contract 4)
  POST   /accounts/{guid}/transactions → initiate bank transaction (Contract 5)
    POST   /accounts/{guid}/exchanges    → initiate currency exchange (Contract 6)
"""

from accounts import views
from django.urls import path  # type: ignore[import-untyped]

urlpatterns = [
    path("", views.account_list, name="account_list"),
    path(
        "<uuid:guid>/",
        views.account_detail,
        name="account_detail",
    ),
    path(
        "<uuid:guid>/transactions/",
        views.account_transactions,
        name="account_transactions",
    ),
    path(
        "<uuid:guid>/exchanges/",
        views.account_exchanges,
        name="account_exchanges",
    ),
]
