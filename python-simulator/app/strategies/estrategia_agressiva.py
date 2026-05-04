from app.strategies.estrategia_base import BaseStrategy

class EstrategiaAgressiva(BaseStrategy):
    """
    Modelo agressivo: alto risco, projeções otimistas.
    Usa taxas de juro altas e inflação baixa.
    """

    def simular(self, dados: dict) -> dict:
        # TODO: implementar lógica agressiva no Sprint C
        return {
            "estrategia": "agressiva",
            "status": "nao_implementado"
        }