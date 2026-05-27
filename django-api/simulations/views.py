import logging

from rest_framework import serializers
from rest_framework.decorators import api_view
from rest_framework.response import Response

from simulations.strategies import ConservativeStrategy, ModerateStrategy, AggressiveStrategy
from simulations.scenarios import StudentScenario, SingleParentScenario, CoupleScenario

logger = logging.getLogger(__name__)


class SimulateSerializer(serializers.Serializer):
    strategy = serializers.ChoiceField(
        choices=['conservative', 'moderate', 'aggressive'],
        default='moderate'
    )


class ScenarioSerializer(serializers.Serializer):
    type = serializers.ChoiceField(
        choices=['student', 'couple', 'single_parent'],
        default='student'
    )


class WelcomeSerializer(serializers.Serializer):
    mensagem   = serializers.CharField()
    linguagem  = serializers.CharField()
    status     = serializers.CharField()
    timestamp  = serializers.CharField()



@api_view(['GET'])
def welcome(request):
    from datetime import datetime

    logger.info("Welcome endpoint requested %s", datetime.now())

    data = {
        'mensagem': 'Bem vindo à API do DEC - Dinheiro Em Casa!',
        'linguagem': 'Django REST Framework',
        'status': 'Ok Running!',
        'timestamp': datetime.now().isoformat()
    }

    serializer = WelcomeSerializer(data)
    logger.debug("Welcome response built — timestamp: %s", data["timestamp"])

    return Response(serializer.data)




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
        scenario = CoupleScenario()
    elif scenario_type == 'single_parent':
        scenario = SingleParentScenario()
    else:
        scenario = StudentScenario()

    result = scenario.create_scenario(request.data)
    logger.info("Scenario created — type: %s", scenario_type)
    return Response(result)
