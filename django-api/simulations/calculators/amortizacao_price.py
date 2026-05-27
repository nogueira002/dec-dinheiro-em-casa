# TODO: implement amortização Price calculator



def calcular_amortizacao(emprestimo: float, tan: float, meses: int) ->dict:
    # R = PV × [i / (1 - (1 + i) ^ -n)]
    # R  = Prestação mensal constante
    # PV = Valor do empréstimo
    # i  = Taxa de juro mensal (TAN / 12)
    # n  = Número de prestações (meses)

    if emprestimo <= 0:
        raise ValueError("O emprestimo deve ser maior que zero.")
    if tan < 0:
        raise ValueError("A taxa de juro não pode ser negativa.")
    if meses <= 0:
        raise ValueError("O número de meses deve ser maior que zero.")

    i = tan / 12
    prestacao_mensal_const = emprestimo * (i / (1 - (1 + i) ** -meses))
    custo_total = prestacao_mensal_const * meses
    juros_totais = (prestacao_mensal_const * meses) - emprestimo

    return {
        "prestacao_mensal": round(prestacao_mensal_const, 2),
        "custo_total": round(custo_total, 2),
        "juros_totais": round(juros_totais, 2)
    }

