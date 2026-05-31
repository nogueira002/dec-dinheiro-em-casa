import json
from django.test import TestCase

class TestObjectivesView(TestCase):

    def setUp(self):
        self.valid_data = {
            "scenario_id": 1,
            "description": "Poupar para férias",
            "targetvalue": 1000.00,
            "termmonths": 6,
            "current_value": 0
        }

    def test_create_objective_sucesso(self):
        response = self.client.post(
            '/api/objectives/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 201)

    def test_create_objective_dados_invalidos(self):
        response = self.client.post(
            '/api/objectives/',
            data=json.dumps({}),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 400)

    def test_list_objectives_sucesso(self):
        self.client.post(
            '/api/objectives/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        response = self.client.get('/api/objectives/?scenario_id=1')
        self.assertEqual(response.status_code, 200)
        self.assertIsInstance(response.json(), list)

    def test_list_objectives_sem_scenario_id(self):
        response = self.client.get('/api/objectives/')
        self.assertEqual(response.status_code, 400)