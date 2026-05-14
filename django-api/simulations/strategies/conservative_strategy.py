import logging
from simulations.strategies.base_strategy import BaseStrategy

logger = logging.getLogger(__name__)


class ConservativeStrategy(BaseStrategy):
    """
    Conservative model: low risk, pessimistic projections.
    Uses low interest rates and high inflation.
    """

    def simulate(self, data: dict) -> dict:
        logger.debug("ConservativeStrategy.simulate called — data: %s", data)
        # TODO: implement conservative logic in Sprint C
        result = {
            "strategy": "conservative",
            "status": "not_implemented"
        }
        logger.info("ConservativeStrategy result: %s", result)
        return result
