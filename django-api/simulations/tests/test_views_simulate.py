import json
from django.test import TestCase

class TestSimulateView(TestCase):

    def setUp(self):
        self.valid_data = {
            "scenario_id": 1,
            "strategy": "moderate",
            "rendimento_mensal": 2000,
            "despesas_mensais": 1400,
            "encargos_fixos": 600,
            "capital_poupanca": 1000,
            "emprestimo": 10000,
            "meses": 12
        }

    def test_simulate_moderate_sucesso(self):
        response = self.client.post(
            '/api/simulate/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 200)
        self.assertIn("taxa_esforco", response.json())
        self.assertIn("projecao", response.json())

    def test_simulate_conservative_sucesso(self):
        data = {**self.valid_data, "strategy": "conservative"}
        response = self.client.post(
            '/api/simulate/',
            data=json.dumps(data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["strategy"], "conservative")

    def test_simulate_aggressive_sucesso(self):
        data = {**self.valid_data, "strategy": "aggressive"}
        response = self.client.post(
            '/api/simulate/',
            data=json.dumps(data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 200)
        self.assertEqual(response.json()["strategy"], "aggressive")

    def test_simulate_strategy_invalida(self):
        data = {**self.valid_data, "strategy": "invalida"}
        response = self.client.post(
            '/api/simulate/',
            data=json.dumps(data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 400)