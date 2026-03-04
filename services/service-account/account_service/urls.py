from django.urls import path # type: ignore
from accounts import views

urlpatterns = [
    path('', views.root, name='root'),
    path('health/', views.health, name='health'),
]