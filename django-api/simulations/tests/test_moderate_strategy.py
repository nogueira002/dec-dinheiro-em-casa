import unittest
from simulations.strategies.moderate_strategy import ModerateStrategy

class TestModerateStrategy(unittest.TestCase):

    def setUp(self):
        self.strategy = ModerateStrategy()
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

        self.assertEqual(resultado["strategy"], "moderate")
        self.assertAlmostEqual(resultado["taxa_esforco"]["taxa_esforco"], 30.0, places=2)
        self.assertEqual(resultado["taxa_esforco"]["classificacao"], "Equilibrado")
        self.assertAlmostEqual(resultado["capitalizacao"]["montante_final"], 1030.42, places=2)
        self.assertAlmostEqual(resultado["capitalizacao"]["juros_gerados"], 30.42, places=2)
        self.assertAlmostEqual(resultado["credito"]["prestacao_mensal"], 853.79, places=2)
        self.assertAlmostEqual(resultado["credito"]["custo_total"], 10245.42, places=2)
        self.assertAlmostEqual(resultado["credito"]["juros_totais"], 245.42, places=2)
        self.assertAlmostEqual(resultado["projecao"]["poupanca_acumulada"], 4203.38, places=2)
        self.assertAlmostEqual(resultado["projecao"]["saldo_medio_mensal"], 350.28, places=2)
        self.assertEqual(resultado["projecao"]["break_even_mes"], 1)

    def test_simulate_sem_credito(self):
        resultado = self.strategy.simulate(self.data_sem_credito)

        self.assertEqual(resultado["strategy"], "moderate")
        self.assertIsNone(resultado["credito"])
        self.assertIsNotNone(resultado["capitalizacao"])

    def test_simulate_sem_poupanca(self):
        resultado = self.strategy.simulate(self.data_sem_poupanca)

        self.assertEqual(resultado["strategy"], "moderate")
        self.assertIsNone(resultado["capitalizacao"])
        self.assertIsNotNone(resultado["credito"])