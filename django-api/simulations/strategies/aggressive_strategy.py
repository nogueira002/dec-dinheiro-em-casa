import logging
from simulations.strategies.base_strategy import BaseStrategy

logger = logging.getLogger(__name__)


class AggressiveStrategy(BaseStrategy):
    """
    Aggressive model: high risk, optimistic projections.
    Uses high interest rates and low inflation.
    """

    def simulate(self, data: dict) -> dict:
        logger.debug("AggressiveStrategy.simulate called — data: %s", data)
        # TODO: implement aggressive logic in Sprint C
        result = {
            "strategy": "aggressive",
            "status": "not_implemented"
        }
        logger.info("AggressiveStrategy result: %s", result)
        return result
