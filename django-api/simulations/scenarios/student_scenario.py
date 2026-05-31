import logging
from simulations.scenarios.base_scenario import BaseScenario

logger = logging.getLogger(__name__)


class StudentScenario(BaseScenario):
    """
    Creates scenario for a displaced student.
    Low income, housing and food expenses predominant.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("StudentScenario.create_scenario called — data: %s", data)

        result = {
            "type": "student",
            "status": "not_implemented"
        }
        logger.info("StudentScenario result: %s", result)
        return result
