from abc import ABC, abstractmethod

class BaseStrategy(ABC):
    """
    Interface base para os modelos de simulação financeira.
    Implementações: EstragiaConservadora, EstrategiaModerada, EstragiaAgressiva
    """

    @abstractmethod
    def simular(self, dados: dict) -> dict:
        pass
