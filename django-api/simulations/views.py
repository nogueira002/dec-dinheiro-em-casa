from django.http import JsonResponse
from django.views.decorators.csrf import csrf_exempt
from django.views.decorators.http import require_http_methods
import json

from simulations.strategies import ConservativeStrategy, ModerateStrategy, AggressiveStrategy
from simulations.factories import StudentFactory, SingleParentFactory, CoupleFactory


@require_http_methods(["GET"])
def health_check(request):
    return JsonResponse({
        "status": "ok",
        "service": "django-api",
        "version": "1.0.0"
    })


@csrf_exempt
@require_http_methods(["POST"])
def simulate(request):
    try:
        data = json.loads(request.body)
        strategy_type = data.get('strategy', 'moderate')

        if strategy_type == 'conservative':
            strategy = ConservativeStrategy()
        elif strategy_type == 'aggressive':
            strategy = AggressiveStrategy()
        else:
            strategy = ModerateStrategy()

        result = strategy.simulate(data)
        return JsonResponse(result)

    except Exception as e:
        return JsonResponse({"error": str(e)}, status=400)


@csrf_exempt
@require_http_methods(["POST"])
def create_scenario(request):
    try:
        data = json.loads(request.body)
        scenario_type = data.get('type', 'student')

        if scenario_type == 'couple':
            factory = CoupleFactory()
        elif scenario_type == 'single_parent':
            factory = SingleParentFactory()
        else:
            factory = StudentFactory()

        result = factory.create_scenario(data)
        return JsonResponse(result)

    except Exception as e:
        return JsonResponse({"error": str(e)}, status=400)
