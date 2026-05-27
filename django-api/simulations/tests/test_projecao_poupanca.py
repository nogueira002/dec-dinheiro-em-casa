import unittest
from simulations.calculators.projecao_poupanca import calcular_projecao_poupanca

class TestProjecaoPoupanca(unittest.TestCase):

    def test_calculo_correto(self):
        rendimento = 2000
        despesas = 1400
        taxa_inflacao = 0.025
        meses = 12

        resultado = calcular_projecao_poupanca(rendimento, despesas, taxa_inflacao, meses)

        self.assertIsNotNone(resultado["poupanca_acumulada"])
        self.assertIsNotNone(resultado["saldo_medio_mensal"])
        self.assertIsNotNone(resultado["break_even_mes"])
        self.assertGreater(resultado["poupanca_acumulada"], 0)

    def test_despesas_maiores_rendimento(self):
        rendimento = 1000
        despesas = 1500
        taxa_inflacao = 0.025
        meses = 12

        resultado = calcular_projecao_poupanca(rendimento, despesas, taxa_inflacao, meses)

        self.assertLess(resultado["poupanca_acumulada"], 0)
        self.assertIsNone(resultado["break_even_mes"])

    def test_rendimento_zero(self):
        with self.assertRaises(ValueError):
            calcular_projecao_poupanca(0, 1400, 0.025, 12)

    def test_despesas_negativas(self):
        with self.assertRaises(ValueError):
            calcular_projecao_poupanca(2000, -500, 0.025, 12)

    def test_meses_zero(self):
        with self.assertRaises(ValueError):
            calcular_projecao_poupanca(2000, 1400, 0.025, 0)