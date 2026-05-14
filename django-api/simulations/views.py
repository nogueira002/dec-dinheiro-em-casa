import logging

from rest_framework.decorators import api_view
from rest_framework.response import Response

from simulations.serializers import SimulateSerializer, ScenarioSerializer
from simulations.strategies import ConservativeStrategy, ModerateStrategy, AggressiveStrategy
from simulations.factories import StudentFactory, SingleParentFactory, CoupleFactory

logger = logging.getLogger(__name__)


@api_view(['GET'])
def health_check(request):
    logger.info("Health check requested")
    return Response({
        "status": "ok",
        "service": "django-api",
        "version": "1.0.0"
    })


@api_view(['POST'])
def simulate(request):
    logger.info("Simulation request received — data: %s", request.data)

    serializer = SimulateSerializer(data=request.data)

    if not serializer.is_valid():
        logger.warning("Invalid simulation data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    strategy_type = serializer.validated_data['strategy']
    logger.debug("Strategy selected: %s", strategy_type)

    if strategy_type == 'conservative':
        strategy = ConservativeStrategy()
    elif strategy_type == 'aggressive':
        strategy = AggressiveStrategy()
    else:
        strategy = ModerateStrategy()

    result = strategy.simulate(request.data)
    logger.info("Simulation completed — strategy: %s", strategy_type)
    return Response(result)


@api_view(['POST'])
def create_scenario(request):
    logger.info("Scenario request received — data: %s", request.data)

    serializer = ScenarioSerializer(data=request.data)

    if not serializer.is_valid():
        logger.warning("Invalid scenario data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    scenario_type = serializer.validated_data['type']
    logger.debug("Scenario type selected: %s", scenario_type)

    if scenario_type == 'couple':
        factory = CoupleFactory()
    elif scenario_type == 'single_parent':
        factory = SingleParentFactory()
    else:
        factory = StudentFactory()

    result = factory.create_scenario(request.data)
    logger.info("Scenario created — type: %s", scenario_type)
    return Response(result)
