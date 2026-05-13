from simulations.factories.base_factory import BaseFactory

class CoupleFactory(BaseFactory):
    """
    Creates scenario for a couple.
    Two incomes, shared expenses.
    """

    def create_scenario(self, data: dict) -> dict:
        # TODO: implement logic in Sprint C
        return {
            "type": "couple",
            "status": "not_implemented"
        }
