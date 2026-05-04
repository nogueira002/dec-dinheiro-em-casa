from abc import ABC, abstractmethod


class BaseFactory(ABC):
    """
    Interface base para criação de cenários familiares.
    Implementações: FabricaEstudante, FabricaFamiliaMonoparental, FabricaCasal
    """

    @abstractmethod
    def criar_cenario(self, dados: dict) -> dict:
        """
        Cria um cenário familiar com base nos dados fornecidos.

        :param dados: Dicionário com os dados do agregado familiar
        :return: Dicionário com o cenário criado
        """
        pass