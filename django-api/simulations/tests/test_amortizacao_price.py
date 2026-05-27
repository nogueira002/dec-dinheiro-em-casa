import unittest
from simulations.calculators.amortizacao_price import calcular_amortizacao

class  TestCalcularAmortizacao(unittest.TestCase):
    def test_calculo_correto(self):
        emprestimo = 10000
        tan = 0.045
        meses = 24

        resultado = calcular_amortizacao(emprestimo, tan, meses)

        self.assertAlmostEqual(resultado['prestacao_mensal'], 436.48, places=2)
        self.assertAlmostEqual(resultado["custo_total"], 10475.47, places=2)
        self.assertAlmostEqual(resultado["juros_totais"], 475.47, places=2)

    def test_emprestimo_zero(self):
        with self.assertRaises(ValueError):
            calcular_amortizacao(0, 0.045, 12)

    def test_tan_negativa(self):
        with self.assertRaises(ValueError):
            calcular_amortizacao(1000, -0.045, 12)

    def test_meses_zero(self):
        with self.assertRaises(ValueError):
            calcular_amortizacao(1000, 0.045, 0)


