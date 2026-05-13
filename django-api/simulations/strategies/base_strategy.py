from abc import ABC, abstractmethod

class BaseStrategy(ABC):
    """
    Base interface for financial simulation models.
    Implementations: ConservativeStrategy, ModerateStrategy, AggressiveStrategy
    """

    @abstractmethod
    def simulate(self, data: dict) -> dict:
        pass
