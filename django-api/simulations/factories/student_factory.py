import logging
from simulations.factories.base_factory import BaseFactory

logger = logging.getLogger(__name__)


class StudentFactory(BaseFactory):
    """
    Creates scenario for a displaced student.
    Low income, housing and food expenses predominant.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("StudentFactory.create_scenario called — data: %s", data)
        # TODO: implement logic in Sprint C
        result = {
            "type": "student",
            "status": "not_implemented"
        }
        logger.info("StudentFactory result: %s", result)
        return result
