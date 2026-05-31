import logging
from simulations.scenarios.base_scenario import BaseScenario

logger = logging.getLogger(__name__)


class SingleParentScenario(BaseScenario):
    """
    Creates scenario for a single-parent family.
    One income, expenses with children and housing.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("SingleParentScenario.create_scenario called — data: %s", data)

        result = {
            "type": "single_parent",
            "status": "not_implemented"
        }
        logger.info("SingleParentScenario result: %s", result)
        return result
