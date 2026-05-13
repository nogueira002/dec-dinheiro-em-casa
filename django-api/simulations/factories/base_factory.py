from abc import ABC, abstractmethod

class BaseFactory(ABC):
    """
    Base interface for creating household scenarios.
    Implementations: StudentFactory, SingleParentFactory, CoupleFactory
    """

    @abstractmethod
    def create_scenario(self, data: dict) -> dict:
        pass
