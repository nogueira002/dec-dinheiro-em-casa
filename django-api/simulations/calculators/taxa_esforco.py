# TODO: implement taxa de esforço calculato

# Formula: Taxa de Esforço = (Total de Encargos Mensais / Rendimento Mensal Líquido) * 100

def calcular_taxa_esforco(encargos_fixos: float, rendimento_liquido: float) -> dict:

    if rendimento_liquido <= 0:
        raise ValueError("O rendimento líquido tem que ser maior que zero.")

    taxa_esforco = (encargos_fixos/rendimento_liquido) * 100

    if taxa_esforco < 30:
        classificacao = "Confortável"
    elif taxa_esforco <= 35:
        classificacao = "Equilibrado"
    elif taxa_esforco <= 50:
        classificacao = "Preocupante"
    else:
        classificacao = "Risco Elevado"

    return {
        "taxa_esforco": round(taxa_esforco, 2),
        "classificacao": classificacao
    }