from app.factories.fabrica_base import BaseFactory

class FabricaFamiliaMonoparental(BaseFactory):
    """
    Cria cenário para família monoparental.
    Um rendimento, despesas com filhos e habitação.
    """

    def criar_cenario(self, dados: dict) -> dict:
        # TODO: implementar lógica no Sprint C
        return {
            "tipo": "familia_monoparental",
            "status": "nao_implementado"
        }