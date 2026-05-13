from abc import ABC, abstractmethod

class BaseFactory(ABC):
    """
    Interface base para criação de cenários familiares.
    Implementações: FabricaEstudante, FabricaFamiliaMonoparental, FabricaCasal
    """

    @abstractmethod
    def criar_cenario(self, dados: dict) -> dict:
        pass
