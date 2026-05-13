from django.http import JsonResponse
from django.views.decorators.csrf import csrf_exempt
from django.views.decorators.http import require_http_methods
import json

from simulations.strategies import (
    EstragiaConservadora,
    EstrategiaModerada,
    EstragiaAgressiva
)
from simulations.factories import (
    FabricaEstudante,
    FabricaFamiliaMonoparental,
    FabricaCasal
)

@require_http_methods(["GET"])
def health_check(request):
    return JsonResponse({
        "status": "ok",
        "service": "django-api",
        "version": "1.0.0"
    })

@csrf_exempt
@require_http_methods(["POST"])
def simular(request):
    try:
        dados = json.loads(request.body)
        tipo = dados.get('estrategia', 'moderada')

        if tipo == 'conservadora':
            strategy = EstragiaConservadora()
        elif tipo == 'agressiva':
            strategy = EstragiaAgressiva()
        else:
            strategy = EstrategiaModerada()

        resultado = strategy.simular(dados)
        return JsonResponse(resultado)

    except Exception as e:
        return JsonResponse({"erro": str(e)}, status=400)

@csrf_exempt
@require_http_methods(["POST"])
def criar_cenario(request):
    try:
        dados = json.loads(request.body)
        tipo = dados.get('tipo', 'estudante')

        if tipo == 'casal':
            factory = FabricaCasal()
        elif tipo == 'familia_monoparental':
            factory = FabricaFamiliaMonoparental()
        else:
            factory = FabricaEstudante()

        resultado = factory.criar_cenario(dados)
        return JsonResponse(resultado)

    except Exception as e:
        return JsonResponse({"erro": str(e)}, status=400)
