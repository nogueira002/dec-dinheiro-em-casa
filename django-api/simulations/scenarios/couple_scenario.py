import logging
from simulations.scenarios.base_scenario import BaseScenario

logger = logging.getLogger(__name__)


class CoupleScenario(BaseScenario):
    """
    Creates scenario for a couple.
    Two incomes, shared expenses.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("CoupleScenario.create_scenario called — data: %s", data)

        result = {
            "type": "couple",
            "status": "not_implemented"
        }
        logger.info("CoupleScenario result: %s", result)
        return result
