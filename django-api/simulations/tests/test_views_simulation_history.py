import json
from django.test import TestCase

class TestSimulationHistoryView(TestCase):

    def setUp(self):
        self.valid_data = {
            "scenario_id": 1,
            "simulation_type": "moderate",
            "jsonresults": {"taxa_esforco": 30.0, "poupanca_acumulada": 4203.38}
        }

    def test_create_simulation_history_sucesso(self):
        response = self.client.post(
            '/api/simulation-history/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 201)

    def test_create_simulation_history_dados_invalidos(self):
        response = self.client.post(
            '/api/simulation-history/',
            data=json.dumps({}),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 400)

    def test_list_simulation_history_sucesso(self):
        self.client.post(
            '/api/simulation-history/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        response = self.client.get('/api/simulation-history/?scenario_id=1')
        self.assertEqual(response.status_code, 200)
        self.assertIsInstance(response.json(), list)

    def test_list_simulation_history_sem_scenario_id(self):
        response = self.client.get('/api/simulation-history/')
        self.assertEqual(response.status_code, 400)