from simulations.strategies.base_strategy import BaseStrategy

class ConservativeStrategy(BaseStrategy):
    """
    Conservative model: low risk, pessimistic projections.
    Uses low interest rates and high inflation.
    """

    def simulate(self, data: dict) -> dict:
        # TODO: implement conservative logic in Sprint C
        return {
            "strategy": "conservative",
            "status": "not_implemented"
        }
