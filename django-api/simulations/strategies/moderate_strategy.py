from simulations.strategies.base_strategy import BaseStrategy

class ModerateStrategy(BaseStrategy):
    """
    Moderate model: balanced risk, realistic projections.
    Uses average interest rates and inflation.
    """

    def simulate(self, data: dict) -> dict:
        # TODO: implement moderate logic in Sprint C
        return {
            "strategy": "moderate",
            "status": "not_implemented"
        }
