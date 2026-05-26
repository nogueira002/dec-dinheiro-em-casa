# TODO: implement capitalização composta calculator


def calcular_capitalizacao_composta(capital: float, tan: float, meses: int) -> dict:

    if capital <= 0:
        raise ValueError("O capital deve ser maior que zero.")
    if tan < 0:
        raise ValueError("A taxa de juro não pode ser negativa.")
    if meses <= 0:
        raise ValueError("O número de meses deve ser maior que zero.")

    i = tan / 12
    montante_final = capital * (1 + i) ** meses

    return {
        "montante_final": round(montante_final, 2),
        "juros_gerados": round(montante_final - capital, 2)
    }


