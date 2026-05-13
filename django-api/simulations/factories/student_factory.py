from simulations.factories.base_factory import BaseFactory

class StudentFactory(BaseFactory):
    """
    Creates scenario for a displaced student.
    Low income, housing and food expenses predominant.
    """

    def create_scenario(self, data: dict) -> dict:
        # TODO: implement logic in Sprint C
        return {
            "type": "student",
            "status": "not_implemented"
        }
