import logging

from simulations.strategies.base_strategy import BaseStrategy

from simulations.calculators.taxa_esforco import calcular_taxa_esforco
from simulations.calculators.capitalizacao_composta import calcular_capitalizacao_composta
from simulations.calculators.amortizacao_price import calcular_amortizacao
from simulations.calculators.projecao_poupanca import calcular_projecao_poupanca


logger = logging.getLogger(__name__)


class ModerateStrategy(BaseStrategy):
    """
    Moderate model: balanced risk, realistic projections.
    Uses average interest rates and inflation.
    """

    TAXA_JURO_POUPANCA = 0.03
    TAXA_INFLACAO = 0.025
    TAXA_JURO_CREDITO = 0.045

    def simulate(self, data:dict) -> dict:
        logger.debug("ModerateStrategy.simulate called — data: %s", data)

        rendimento_mensal = data.get("rendimento_mensal", 0)
        despesas_mensais = data.get("despesas_mensais", 0)
        encargos_fixos = data.get("encargos_fixos", 0)
        capital_poupanca = data.get("capital_poupanca", 0)
        emprestimo = data.get("emprestimo", 0)
        meses = data.get("meses", 12)

        # Calcula a taxa de esforço
        # Usa encargos_fixos e rendimento_mensal
        taxa_esforco = calcular_taxa_esforco(encargos_fixos, rendimento_mensal)

        # Calcula capitalização composta —> só se houver capital para poupar
        # Usa a taxa de juro de poupança DESTA estratégia
        capitalizacao = calcular_capitalizacao_composta(
            capital_poupanca, self.TAXA_JURO_POUPANCA, meses
        ) if capital_poupanca > 0 else None

        # Calcula amortização de crédito —> só se houver empréstimo
        # Usa a taxa de juro de crédito DESTA estratégia
        credito = calcular_amortizacao(
            emprestimo, self.TAXA_JURO_CREDITO, meses
        ) if emprestimo > 0 else None

        # Calcula projeção de poupança
        # Usa a taxa de inflação DESTA estratégia
        projecao = calcular_projecao_poupanca(
            rendimento_mensal, despesas_mensais, self.TAXA_INFLACAO, meses
        )

        # Monta o resultado final com todos os cálculos
        result = {
            "strategy": "moderate",
            "taxa_esforco": taxa_esforco,
            "capitalizacao": capitalizacao,
            "credito": credito,
            "projecao": projecao
        }

        logger.info("ModerateStrategy result: %s", result)
        return result
