import logging
from simulations.factories.base_factory import BaseFactory

logger = logging.getLogger(__name__)


class SingleParentFactory(BaseFactory):
    """
    Creates scenario for a single-parent family.
    One income, expenses with children and housing.
    """

    def create_scenario(self, data: dict) -> dict:
        logger.debug("SingleParentFactory.create_scenario called — data: %s", data)
        # TODO: implement logic in Sprint C
        result = {
            "type": "single_parent",
            "status": "not_implemented"
        }
        logger.info("SingleParentFactory result: %s", result)
        return result
