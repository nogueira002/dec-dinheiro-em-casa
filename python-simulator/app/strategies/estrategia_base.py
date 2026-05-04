from abc import ABC, abstractmethod


class BaseStrategy(ABC):
    """
    Interface base para os modelos de simulação financeira.
    Implementações: EstategiaConservadora, EstrategiaModelada, EstrategiaAgressiva
    """

    @abstractmethod
    def simular(self, dados: dict) -> dict:
        """
        Executa a simulação financeira com base nos dados fornecidos.

        :param dados: Dicionário com os dados do cenário (rendimentos, despesas)
        :return: Dicionário com os resultados da simulação
        """
        pass