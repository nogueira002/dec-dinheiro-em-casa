import unittest
from  simulations.calculators.taxa_esforco import calcular_taxa_esforco

class TestTaxaEsforco(unittest.TestCase):
    def test_situacao_confortavel(self):
        encargos = 500
        rendimento = 2000

        resultado = calcular_taxa_esforco(encargos, rendimento)

        self.assertEqual(resultado['taxa_esforco'], 25.0)
        self.assertEqual(resultado["classificacao"], "Confortável")

    def test_situacao_equilibrada(self):
        encargos = 620
        rendimento = 2000

        resultado = calcular_taxa_esforco(encargos, rendimento)
        self.assertEqual(resultado['taxa_esforco'], 31.0)
        self.assertEqual(resultado["classificacao"], "Equilibrado")

    def test_situacao_preocupante(self):
        encargos = 800
        rendimento = 2000

        resultado = calcular_taxa_esforco(encargos, rendimento)
        self.assertEqual(resultado['taxa_esforco'], 40.0)
        self.assertEqual(resultado["classificacao"], "Preocupante")

    def test_risco_elevado(self):
        encargos = 1100
        rendimento = 2000

        resultado = calcular_taxa_esforco(encargos, rendimento)
        self.assertEqual(resultado['taxa_esforco'], 55.0)
        self.assertEqual(resultado["classificacao"], "Risco Elevado")

    def test_rendimento_zero(self):
        with self.assertRaises(ValueError):
            calcular_taxa_esforco(500, 0)





