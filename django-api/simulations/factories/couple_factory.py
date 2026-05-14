import logging
from simulations.factories.base_factory import BaseFactory

logger = logging.getLogger(__name__)


class CoupleFactory(BaseFactory):
    """
    Creates scenario for a couple.
    Two incomes, shared expenses.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("CoupleFactory.create_scenario called — data: %s", data)
        # TODO: implement logic in Sprint C
        result = {
            "type": "couple",
            "status": "not_implemented"
        }
        logger.info("CoupleFactory result: %s", result)
        return result
