from app.strategies.estrategia_base import BaseStrategy

class EstrategiaModerada(BaseStrategy):
    """
    Modelo moderado: risco equilibrado, projeções realistas.
    Usa taxas de juro e inflação médias.
    """

    def simular(self, dados: dict) -> dict:
        # TODO: implementar lógica moderada no Sprint C
        return {
            "estrategia": "moderada",
            "status": "nao_implementado"
        }