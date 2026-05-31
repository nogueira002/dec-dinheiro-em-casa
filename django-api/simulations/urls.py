from django.urls import path
from simulations import views

urlpatterns = [
    path('',                    views.welcome,           name='welcome'),
    path('taxa-esforco/',       views.taxa_esforco,      name='taxa_esforco'),
    path('capitalizacao/',      views.capitalizacao,     name='capitalizacao'),
    path('simular-credito/',    views.simular_credito,   name='simular_credito'),
    path('projecao-poupanca/',  views.projecao_poupanca, name='projecao_poupanca'),
    path('entries/',            views.entries,           name='entries'),
    path('objectives/',         views.objectives,        name='objectives'),

]
