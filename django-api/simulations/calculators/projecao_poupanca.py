def calcular_projecao_poupanca(rendimento_mensal: float, despesas_mensais: float, taxa_inflacao: float, meses: int) -> dict:


    if rendimento_mensal <= 0:
        raise ValueError("O rendimento mensal deve ser maior que zero.")
    if despesas_mensais < 0:
        raise ValueError("As despesas mensais não podem ser negativas.")
    if meses <= 0:
        raise ValueError("O número de meses deve ser maior que zero.")

    poupanca_acumulada = 0
    break_even_mes = None
    saldos = []

    for mes in range(1, meses + 1):
        despesa_ajustada = despesas_mensais * (1 + taxa_inflacao) ** mes

        saldo_mes = rendimento_mensal - despesa_ajustada

        poupanca_acumulada +=  saldo_mes

        # Guarda o saldo do mês
        saldos.append(saldo_mes)

        # Regista o primeiro mês com poupança positiva
        if break_even_mes is None and poupanca_acumulada > 0:
            break_even_mes = mes

    return {
        "poupanca_acumulada": round(poupanca_acumulada, 2),
        "saldo_medio_mensal": round(sum(saldos) / len(saldos), 2),
        "break_even_mes": break_even_mes
    }