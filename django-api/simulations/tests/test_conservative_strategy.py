import unittest
from simulations.strategies.conservative_strategy import ConservativeStrategy

class TestConservativeStrategy(unittest.TestCase):

    def setUp(self):
        self.strategy = ConservativeStrategy()
        self.data_completo = {
            "rendimento_mensal": 2000,
            "despesas_mensais": 1400,
            "encargos_fixos": 600,
            "capital_poupanca": 1000,
            "emprestimo": 10000,
            "meses": 12
        }
        self.data_sem_credito = {
            "rendimento_mensal": 2000,
            "despesas_mensais": 1400,
            "encargos_fixos": 600,
            "capital_poupanca": 1000,
            "meses": 12
        }
        self.data_sem_poupanca = {
            "rendimento_mensal": 2000,
            "despesas_mensais": 1400,
            "encargos_fixos": 600,
            "emprestimo": 10000,
            "meses": 12
        }

    def test_simulate_completo(self):
        resultado = self.strategy.simulate(self.data_completo)

        self.assertEqual(resultado["strategy"], "conservative")
        self.assertAlmostEqual(resultado["taxa_esforco"]["taxa_esforco"], 30.0, places=2)
        self.assertEqual(resultado["taxa_esforco"]["classificacao"], "Equilibrado")
        self.assertAlmostEqual(resultado["capitalizacao"]["montante_final"], 1015.1, places=2)
        self.assertAlmostEqual(resultado["capitalizacao"]["juros_gerados"], 15.1, places=2)
        self.assertAlmostEqual(resultado["credito"]["prestacao_mensal"], 860.66, places=2)
        self.assertAlmostEqual(resultado["credito"]["custo_total"], 10327.97, places=2)
        self.assertAlmostEqual(resultado["credito"]["juros_totais"], 327.97, places=2)
        self.assertAlmostEqual(resultado["projecao"]["poupanca_acumulada"], 2122.43, places=2)
        self.assertAlmostEqual(resultado["projecao"]["saldo_medio_mensal"], 176.87, places=2)
        self.assertEqual(resultado["projecao"]["break_even_mes"], 1)

    def test_simulate_sem_credito(self):
        resultado = self.strategy.simulate(self.data_sem_credito)

        self.assertEqual(resultado["strategy"], "conservative")
        self.assertIsNone(resultado["credito"])
        self.assertIsNotNone(resultado["capitalizacao"])

    def test_simulate_sem_poupanca(self):
        resultado = self.strategy.simulate(self.data_sem_poupanca)

        self.assertEqual(resultado["strategy"], "conservative")
        self.assertIsNone(resultado["capitalizacao"])
        self.assertIsNotNone(resultado["credito"])