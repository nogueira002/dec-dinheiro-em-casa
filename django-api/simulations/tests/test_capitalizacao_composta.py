import unittest
from simulations.calculators.capitalizacao_composta import calcular_capitalizacao_composta

class TestCapitalizacaoComposta(unittest.TestCase):
    def test_calculo_correto (self):
        capital = 1000
        tan = 0.03
        meses = 12

        resultado = calcular_capitalizacao_composta(capital, tan, meses)

        self.assertEqual(resultado["montante_final"], 1030.42)
        self.assertEqual(resultado["juros_gerados"], 30.42)


    def test_capital_zero(self):
        with self.assertRaises(ValueError):
            calcular_capitalizacao_composta(0, 120, 10)


    def test_tan_negativa(self):
        with self.assertRaises(ValueError):
            calcular_capitalizacao_composta(1000, -120, 10)


    def test_meses_zero(self):
        with self.assertRaises(ValueError):
            calcular_capitalizacao_composta(1000, 120, 0)


