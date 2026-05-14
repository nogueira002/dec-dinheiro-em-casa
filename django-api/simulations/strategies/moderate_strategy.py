import logging
from simulations.strategies.base_strategy import BaseStrategy

logger = logging.getLogger(__name__)


class ModerateStrategy(BaseStrategy):
    """
    Moderate model: balanced risk, realistic projections.
    Uses average interest rates and inflation.
    """

    def simulate(self, data: dict) -> dict:
        logger.debug("ModerateStrategy.simulate called — data: %s", data)
        # TODO: implement moderate logic in Sprint C
        result = {
            "strategy": "moderate",
            "status": "not_implemented"
        }
        logger.info("ModerateStrategy result: %s", result)
        return result
