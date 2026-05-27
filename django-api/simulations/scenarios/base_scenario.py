from abc import ABC, abstractmethod

class BaseScenario(ABC):
    """
    Base interface for creating household scenarios.
    Implementations: StudentScenario, SingleParentScenario, CoupleScenario
    """

    @abstractmethod
    def create_scenario(self, data: dict) -> dict:
        pass
