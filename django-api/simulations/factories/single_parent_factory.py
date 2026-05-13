from simulations.factories.base_factory import BaseFactory

class SingleParentFactory(BaseFactory):
    """
    Creates scenario for a single-parent family.
    One income, expenses with children and housing.
    """

    def create_scenario(self, data: dict) -> dict:
        # TODO: implement logic in Sprint C
        return {
            "type": "single_parent",
            "status": "not_implemented"
        }
