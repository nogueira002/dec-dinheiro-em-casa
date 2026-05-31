from django.db import models


class Entry(models.Model):
    TIPO_CHOICES = [
        ('rendimento', 'Rendimento'),
        ('despesa', 'Despesa'),
    ]

    RECURRENCE_CHOICES = [
        ('mensal', 'Mensal'),
        ('anual', 'Anual'),
        ('unica', 'Única'),
    ]

    scenario_id = models.IntegerField()
    type        = models.CharField(max_length=20, choices=TIPO_CHOICES)
    category    = models.CharField(max_length=100)
    amount      = models.DecimalField(max_digits=10, decimal_places=2)
    month       = models.IntegerField()
    year        = models.IntegerField()
    recurrence  = models.CharField(max_length=20, choices=RECURRENCE_CHOICES, default='mensal')

    class Meta:
        db_table = 'entries'

    def __str__(self):
        return f"{self.type} - {self.category} - {self.amount}€"


class Objective(models.Model):
    scenario_id   = models.IntegerField()
    description   = models.CharField(max_length=255)
    targetvalue   = models.DecimalField(max_digits=10, decimal_places=2)
    termmonths    = models.IntegerField()
    current_value = models.DecimalField(max_digits=10, decimal_places=2, default=0.00)

    class Meta:
        db_table = 'objectives'

    def __str__(self):
        return f"{self.description} - {self.targetvalue}€"


class SimulationHistory(models.Model):
    SIMULATION_TYPE_CHOICES = [
        ('conservative', 'Conservative'),
        ('moderate', 'Moderate'),
        ('aggressive', 'Aggressive'),
    ]

    scenario_id     = models.IntegerField()
    executiondate   = models.DateTimeField(auto_now_add=True)
    jsonresults     = models.JSONField()
    simulation_type = models.CharField(max_length=20, choices=SIMULATION_TYPE_CHOICES)

    class Meta:
        db_table = 'simulationshistory'

    def __str__(self):
        return f"Simulation {self.simulation_type} - {self.executiondate}"