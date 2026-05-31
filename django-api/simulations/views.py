import logging

from drf_spectacular.utils import extend_schema
from rest_framework import serializers
from rest_framework.decorators import api_view
from rest_framework.response import Response


from simulations.models import Entry, Objective, SimulationHistory


from simulations.calculators.taxa_esforco import calcular_taxa_esforco
from simulations.calculators.capitalizacao_composta import calcular_capitalizacao_composta
from simulations.calculators.amortizacao_price import calcular_amortizacao
from simulations.calculators.projecao_poupanca import calcular_projecao_poupanca
from simulations.strategies import ConservativeStrategy, AggressiveStrategy, ModerateStrategy

logger = logging.getLogger(__name__)

class WelcomeSerializer(serializers.Serializer):
    mensagem   = serializers.CharField()
    linguagem  = serializers.CharField()
    status     = serializers.CharField()
    timestamp  = serializers.CharField()

class TaxaEsforcoSerializer(serializers.Serializer):
    encargos_fixos     = serializers.FloatField()
    rendimento_liquido = serializers.FloatField()
class CapitalizacaoSerializer(serializers.Serializer):
    capital = serializers.FloatField()
    tan     = serializers.FloatField()
    meses   = serializers.IntegerField()
class SimularCreditoSerializer(serializers.Serializer):
    emprestimo = serializers.FloatField()
    tan        = serializers.FloatField()
    meses      = serializers.IntegerField()
class ProjecaoPoupancaSerializer(serializers.Serializer):
    rendimento_mensal  = serializers.FloatField()
    despesas_mensais   = serializers.FloatField()
    taxa_inflacao      = serializers.FloatField()
    meses              = serializers.IntegerField()

class EntrySerializer(serializers.Serializer):
    scenario_id = serializers.IntegerField()
    type = serializers.CharField()
    category = serializers.CharField()
    amount = serializers.FloatField()
    month = serializers.IntegerField()
    year = serializers.IntegerField()
    recurrence = serializers.CharField()
class ObjectiveSerializer(serializers.Serializer):
    scenario_id = serializers.IntegerField()
    description = serializers.CharField()
    targetvalue = serializers.FloatField()
    termmonths = serializers.IntegerField()
    current_value = serializers.FloatField()
    current_value = serializers.IntegerField()
class SimulationHistorySerializer(serializers.Serializer):
    scenario_id = serializers.IntegerField()
    executiondate = serializers.DateTimeField(read_only=True)
    jsonresults = serializers.JSONField()
    simulation_type = serializers.CharField()

class SimulateSerializer(serializers.Serializer):
    strategy          = serializers.ChoiceField(choices=['conservative', 'moderate', 'aggressive'], default='moderate')
    scenario_id       = serializers.IntegerField(required=False, default=0)
    rendimento_mensal = serializers.FloatField(required=False, default=0)
    despesas_mensais  = serializers.FloatField(required=False, default=0)
    encargos_fixos    = serializers.FloatField(required=False, default=0)
    capital_poupanca  = serializers.FloatField(required=False, default=0)
    emprestimo        = serializers.FloatField(required=False, default=0)
    meses             = serializers.IntegerField(required=False, default=12)


@extend_schema(tags=['Geral'])
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


@extend_schema(tags=['Calculadores'])
@api_view(['POST'])
def taxa_esforco(request):
    logger.info("Taxa esforco request received — data: %s", request.data)

    serializer = TaxaEsforcoSerializer(data=request.data)
    if not serializer.is_valid():
        logger.warning("Invalid taxa esforco data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    try:
        resultado = calcular_taxa_esforco(
            serializer.validated_data['encargos_fixos'],
            serializer.validated_data['rendimento_liquido']
        )
        logger.info("Taxa esforco calculated — result: %s", resultado)
        return Response(resultado)
    except ValueError as e:
        logger.error("Taxa esforco error — %s", str(e))
        return Response({"erro": str(e)}, status=400)


@extend_schema(tags=['Calculadores'])
@api_view(['POST'])
def capitalizacao(request):
    logger.info("Capitalizacao request received — data: %s", request.data)

    serializer = CapitalizacaoSerializer(data=request.data)
    if not serializer.is_valid():
        logger.warning("Invalid capitalizacao data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    try:
        resultado = calcular_capitalizacao_composta(
            serializer.validated_data['capital'],
            serializer.validated_data['tan'],
            serializer.validated_data['meses']
        )
        logger.info("Capitalizacao calculated — result: %s", resultado)
        return Response(resultado)
    except ValueError as e:
        logger.error("Capitalizacao error — %s", str(e))
        return Response({"erro": str(e)}, status=400)


@extend_schema(tags=['Calculadores'])
@api_view(['POST'])
def simular_credito(request):
    logger.info("Simular credito request received — data: %s", request.data)

    serializer = SimularCreditoSerializer(data=request.data)
    if not serializer.is_valid():
        logger.warning("Invalid simular credito data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    try:
        resultado = calcular_amortizacao(
            serializer.validated_data['emprestimo'],
            serializer.validated_data['tan'],
            serializer.validated_data['meses']
        )
        logger.info("Simular credito calculated — result: %s", resultado)
        return Response(resultado)
    except ValueError as e:
        logger.error("Simular credito error — %s", str(e))
        return Response({"erro": str(e)}, status=400)


@extend_schema(tags=['Calculadores'])
@api_view(['POST'])
def projecao_poupanca(request):
    logger.info("Projecao poupanca request received — data: %s", request.data)

    serializer = ProjecaoPoupancaSerializer(data=request.data)
    if not serializer.is_valid():
        logger.warning("Invalid projecao poupanca data — errors: %s", serializer.errors)
        return Response(serializer.errors, status=400)

    try:
        resultado = calcular_projecao_poupanca(
            serializer.validated_data['rendimento_mensal'],
            serializer.validated_data['despesas_mensais'],
            serializer.validated_data['taxa_inflacao'],
            serializer.validated_data['meses']
        )
        logger.info("Projecao poupanca calculated — result: %s", resultado)
        return Response(resultado)
    except ValueError as e:
        logger.error("Projecao poupanca error — %s", str(e))
        return Response({"erro": str(e)}, status=400)


@extend_schema(tags=['Lançamento'])
@api_view(['GET', 'POST'])
def entries(request):
    if request.method == 'POST':
        logger.info("Criar entry — data: %s", request.data)

        serializer = EntrySerializer(data=request.data)

        if not serializer.is_valid():
            logger.warning("Invalid entry data — errors: %s", serializer.errors)
            return Response(serializer.errors, status=400)

        try:
            entry = Entry(**serializer.validated_data)
            entry.save()
            logger.info("Entry criado com sucesso — id: %s", entry.id)
            return Response(EntrySerializer(entry).data, status=201)

        except Exception as e:
            logger.error("Entry error — %s", str(e))
            return Response({"erro": str(e)}, status=400)

    elif request.method == 'GET':
        logger.info("Listar entries — scenario_id: %s", request.query_params.get('scenario_id'))

        scenario_id = request.query_params.get('scenario_id')

        if not scenario_id:
            return Response({"erro": "scenario_id é obrigatório"}, status=400)

        try:
            entries_list = Entry.objects.filter(scenario_id=scenario_id)
            serializer = EntrySerializer(entries_list, many=True)
            logger.info("Entries listados — total: %s", entries_list.count())
            return Response(serializer.data, status=200)

        except Exception as e:
            logger.error("Entry list error — %s", str(e))
            return Response({"erro": str(e)}, status=400)


@extend_schema(tags=['Lançamento'])
@api_view(['GET', 'POST'])
def objectives(request):
    if request.method == 'POST':
        logger.info("Criar objectivos — data: %s", request.data)

        serializer = ObjectiveSerializer(data=request.data)
        if not serializer.is_valid():
            logger.warning("Invalid objective data - errors: %s", serializer.errors)
            return Response(serializer.errors, status=400)

        try:
            objective = Objective(**serializer.validated_data)
            objective.save()
            logger.info("Objectivos criado com sucesso — id: %s", objective.id)
            return Response(ObjectiveSerializer(objective).data, status=201)
        except Exception as e:
            logger.error("Objective error — %s", str(e))
            return Response({"erro": str(e)}, status=400)

    elif request.method == 'GET':
        logger.info("Listar objetivos — scenario_id: %s", request.query_params.get('scenario_id'))

        scenario_id = request.query_params.get('scenario_id')

        if not scenario_id:
            return Response({"erro": "scenario_id é obrigatório"}, status=400)

        try:
            objective_list = Objective.objects.filter(scenario_id=scenario_id)
            serializer = ObjectiveSerializer(objective_list, many=True)
            logger.info("Objectivos listados — total: %s", objective_list.count())
            return Response(serializer.data, status=200)

        except Exception as e:
            logger.error("Objectivo list error — %s", str(e))
            return Response({"erro": str(e)}, status=400)

@extend_schema(tags=['Simulações'])
@api_view(['GET', 'POST'])
def simulation_history(request):
    if request.method == 'POST':
        logger.info("Guardar simulação — data: %s", request.data)

        serializer = SimulationHistorySerializer(data=request.data)
        if not serializer.is_valid():
            logger.warning("Invalid simulation data — errors: %s", serializer.errors)
            return Response(serializer.errors, status=400)

        try:
            simulation = SimulationHistory(**serializer.validated_data)
            simulation.save()
            logger.info("Simulação guardada com sucesso — id: %s", simulation.id)
            return Response(SimulationHistorySerializer(simulation).data, status=201)
        except Exception as e:
            logger.error("Simulation history error — %s", str(e))
            return Response({"erro": str(e)}, status=400)

    elif request.method == 'GET':
        logger.info("Listar histórico — scenario_id: %s", request.query_params.get('scenario_id'))

        scenario_id = request.query_params.get('scenario_id')

        if not scenario_id:
            return Response({"erro": "scenario_id é obrigatório"}, status=400)

        try:
            history_list = SimulationHistory.objects.filter(scenario_id=scenario_id)
            serializer = SimulationHistorySerializer(history_list, many=True)
            logger.info("Histórico listado — total: %s", history_list.count())
            return Response(serializer.data, status=200)

        except Exception as e:
            logger.error("Simulation history list error — %s", str(e))
            return Response({"erro": str(e)}, status=400)





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

    # Guarda automaticamente no histórico
    try:
        SimulationHistory(
            scenario_id=request.data.get('scenario_id', 0),
            simulation_type=strategy_type,
            jsonresults=result
        ).save()
        logger.info("Simulação guardada no histórico — scenario_id: %s", request.data.get('scenario_id'))
    except Exception as e:
        logger.warning("Erro ao guardar histórico — %s", str(e))

    logger.info("Simulation completed — strategy: %s", strategy_type)
    return Response(result)