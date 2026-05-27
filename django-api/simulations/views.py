import logging

from drf_spectacular.utils import extend_schema
from rest_framework import serializers
from rest_framework.decorators import api_view
from rest_framework.response import Response

from simulations.calculators.taxa_esforco import calcular_taxa_esforco
from simulations.calculators.capitalizacao_composta import calcular_capitalizacao_composta
from simulations.calculators.amortizacao_price import calcular_amortizacao
from simulations.calculators.projecao_poupanca import calcular_projecao_poupanca


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