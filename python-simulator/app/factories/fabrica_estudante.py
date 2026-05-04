from app.factories.fabrica_base import BaseFactory

class FabricaEstudante(BaseFactory):
    """
    Cria cenário para estudante deslocado.
    Rendimento baixo, despesas de habitação e alimentação predominantes.
    """

    def criar_cenario(self, dados: dict) -> dict:
        # TODO: implementar lógica no Sprint C
        return {
            "tipo": "estudante",
            "status": "nao_implementado"
        }