from rest_framework import serializers


class SimulateSerializer(serializers.Serializer):
    strategy = serializers.ChoiceField(
        choices=['conservative', 'moderate', 'aggressive'],
        default='moderate'
    )


class ScenarioSerializer(serializers.Serializer):
    type = serializers.ChoiceField(
        choices=['student', 'couple', 'single_parent'],
        default='student'
    )
