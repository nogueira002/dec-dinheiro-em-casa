from django.urls import path
from simulations import views

urlpatterns = [
    path('',              views.welcome,         name='welcome'),
    path('health-check/', views.health_check,   name='health_check'),
    path('simulate/',     views.simulate,        name='simulate'),
    path('scenario/',     views.create_scenario, name='create_scenario'),
]
