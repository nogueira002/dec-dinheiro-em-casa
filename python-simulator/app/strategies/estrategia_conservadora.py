from app.strategies.estrategia_base import BaseStrategy

class EstrategiaConservadora(BaseStrategy):
    """
    Modelo conservador: baixo risco, projeções pessimistas.
    Usa taxas de juro baixas e inflação alta.
    """

    def simular(self, dados: dict) -> dict:
        # TODO: implementar lógica conservadora no Sprint C
        return {
            "estrategia": "conservadora",
            "status": "nao_implementado"
        }