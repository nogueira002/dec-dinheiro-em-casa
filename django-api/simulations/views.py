from rest_framework.decorators import api_view
from rest_framework.response import Response

from simulations.serializers import SimulateSerializer, ScenarioSerializer
from simulations.strategies import ConservativeStrategy, ModerateStrategy, AggressiveStrategy
from simulations.factories import StudentFactory, SingleParentFactory, CoupleFactory


@api_view(['GET'])
def health_check(request):
    return Response({
        "status": "ok",
        "service": "django-api",
        "version": "1.0.0"
    })


@api_view(['POST'])
def simulate(request):
    serializer = SimulateSerializer(data=request.data)

    if not serializer.is_valid():
        return Response(serializer.errors, status=400)

    strategy_type = serializer.validated_data['strategy']

    if strategy_type == 'conservative':
        strategy = ConservativeStrategy()
    elif strategy_type == 'aggressive':
        strategy = AggressiveStrategy()
    else:
        strategy = ModerateStrategy()

    result = strategy.simulate(request.data)
    return Response(result)


@api_view(['POST'])
def create_scenario(request):
    serializer = ScenarioSerializer(data=request.data)

    if not serializer.is_valid():
        return Response(serializer.errors, status=400)

    scenario_type = serializer.validated_data['type']

    if scenario_type == 'couple':
        factory = CoupleFactory()
    elif scenario_type == 'single_parent':
        factory = SingleParentFactory()
    else:
        factory = StudentFactory()

    result = factory.create_scenario(request.data)
    return Response(result)
