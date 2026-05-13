from simulations.factories.fabrica_base import BaseFactory

class FabricaCasal(BaseFactory):
    """
    Cria cenário para casal.
    Dois rendimentos, despesas partilhadas.
    """

    def criar_cenario(self, dados: dict) -> dict:
        # TODO: implementar lógica no Sprint C
        return {
            "tipo": "casal",
            "status": "nao_implementado"
        }
