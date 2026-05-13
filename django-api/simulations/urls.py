from django.urls import path
from simulations import views

urlpatterns = [
    path('health-check/', views.health_check, name='health_check'),
    path('simular/', views.simular, name='simular'),
    path('cenario/', views.criar_cenario, name='criar_cenario'),
]
