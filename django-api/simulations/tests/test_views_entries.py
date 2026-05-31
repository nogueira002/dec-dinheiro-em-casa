import json
import unittest
from django.test import TestCase, RequestFactory
from simulations.views import entries

class TestEntriesView(TestCase):

    def setUp(self):
        self.factory = RequestFactory()
        self.valid_data = {
            "scenario_id": 1,
            "type": "despesa",
            "category": "renda",
            "amount": 620.00,
            "month": 5,
            "year": 2026,
            "recurrence": "mensal"
        }

    def test_create_entry_sucesso(self):
        response = self.client.post(
            '/api/entries/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 201)

    def test_create_entry_dados_invalidos(self):
        response = self.client.post(
            '/api/entries/',
            data=json.dumps({}),
            content_type='application/json'
        )
        self.assertEqual(response.status_code, 400)

    def test_list_entries_sucesso(self):
        self.client.post(
            '/api/entries/',
            data=json.dumps(self.valid_data),
            content_type='application/json'
        )
        response = self.client.get('/api/entries/?scenario_id=1')
        self.assertEqual(response.status_code, 200)
        self.assertIsInstance(response.json(), list)

    def test_list_entries_sem_scenario_id(self):
        response = self.client.get('/api/entries/')
        self.assertEqual(response.status_code, 400)