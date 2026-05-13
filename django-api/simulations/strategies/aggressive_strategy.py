from simulations.strategies.base_strategy import BaseStrategy

class AggressiveStrategy(BaseStrategy):
    """
    Aggressive model: high risk, optimistic projections.
    Uses high interest rates and low inflation.
    """

    def simulate(self, data: dict) -> dict:
        # TODO: implement aggressive logic in Sprint C
        return {
            "strategy": "aggressive",
            "status": "not_implemented"
        }
