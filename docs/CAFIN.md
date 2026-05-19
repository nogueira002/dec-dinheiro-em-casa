# CAFIN – Regras e Glossário Financeiro
## DEC – Dinheiro em Casa

> Documento de referência para implementação e testes do módulo financeiro.
> Versão 1.0 — Sprint B

---

## Índice
1. [Glossário](#glossário)
2. [Regras de Cálculo Financeiro](#regras-de-cálculo-financeiro)
3. [Parâmetros por Tipo de Simulação](#parâmetros-por-tipo-de-simulação)
4. [Exemplos Práticos](#exemplos-práticos)

---

## Glossário

| Termo | Definição |
|---|---|
| **DEC** | Dinheiro em Casa — nome da plataforma |
| **Cenário Familiar** | Perfil virtual criado pelo aluno representando um agregado familiar com rendimentos e despesas |
| **Taxa de Esforço** | Percentagem do rendimento mensal líquido destinada a encargos fixos |
| **TAN** | Taxa Anual Nominal — taxa de juro anual sem considerar capitalização |
| **TANB** | Taxa Anual Nominal Bruta — taxa antes de impostos |
| **Capitalização Composta** | Método de cálculo onde os juros gerados são adicionados ao capital e geram novos juros |
| **Sistema Francês (Price)** | Método de amortização de crédito com prestações constantes |
| **Break-even** | Ponto de equilíbrio — mês em que a poupança acumulada iguala a meta definida |
| **Simulação Conservadora** | Modelo com baixo risco, taxas de juro baixas e inflação alta |
| **Simulação Moderada** | Modelo com risco equilibrado, valores médios de mercado |
| **Simulação Agressiva** | Modelo com alto risco, taxas de juro altas e inflação baixa |
| **Strategy Pattern** | Padrão de design que permite selecionar o algoritmo de simulação em tempo de execução |
| **Factory Pattern** | Padrão de design que encapsula a criação de cenários familiares |
| **dec_educativa** | Base de dados do Portal MVC com dados educativos |
| **dec_financeira** | Base de dados do Django com dados financeiros |
| **Agregado Familiar** | Conjunto de pessoas que vivem na mesma habitação e partilham despesas |
| **Fundo de Emergência** | Reserva financeira equivalente a 3-6 meses de despesas fixas |
| **Rendimento Fixo** | Rendimento mensal garantido (ex: salário) |
| **Rendimento Variável** | Rendimento não garantido que pode variar mensalmente |
| **Despesa Fixa** | Despesa mensal obrigatória e constante (ex: renda, crédito) |
| **Despesa Variável** | Despesa mensal que pode variar (ex: alimentação, lazer) |

---

## Regras de Cálculo Financeiro

### 1. Capitalização Composta
Utilizada para projeção de poupanças e investimentos.
M = C × (1 + i)^n
Onde:
M = Montante final
C = Capital inicial
i = Taxa de juro por período (mensal = TAN / 12)
n = Número de períodos (meses)

**Exemplo:**
- Capital inicial: 1.000€
- TAN: 3%
- Prazo: 12 meses
- i = 3% / 12 = 0.25%
- M = 1.000 × (1 + 0.0025)^12 = 1.030,42€

---

### 2. Taxa de Esforço
Mede a pressão financeira do agregado familiar.
TE = (Encargos Fixos Mensais / Rendimento Líquido Mensal) × 100

**Classificação:**
| Taxa de Esforço | Classificação |
|---|---|
| TE < 30% | Situação confortável |
| 30% ≤ TE ≤ 40% | Situação aceitável |
| TE > 40% | Situação de risco |

**Exemplo:**
- Rendimento líquido mensal: 1.500€
- Encargos fixos: 600€ (renda + crédito)
- TE = (600 / 1.500) × 100 = 40%

---

### 3. Sistema de Amortização Francês (Price)
Utilizado para cálculo de prestações de crédito com prestação constante.
R = PV × [i / (1 - (1 + i)^-n)]
Onde:
R  = Prestação mensal constante
PV = Valor do empréstimo
i  = Taxa de juro mensal (TAN / 12)
n  = Número de prestações (meses)

**Exemplo:**
- Empréstimo: 10.000€
- TAN: 4.5%
- Prazo: 24 meses
- i = 4.5% / 12 = 0.375%
- R = 10.000 × [0.00375 / (1 - (1.00375)^-24)] = 436,35€/mês

---

### 4. Custo Total do Crédito
Custo Total = (R × n) - PV
Onde:
R  = Prestação mensal
n  = Número de prestações
PV = Capital emprestado

**Exemplo (continuação do anterior):**
- Custo Total = (436,35 × 24) - 10.000 = 472,40€

---

### 5. Projeção de Poupança com Inflação
Ajusta as despesas variáveis ao custo de vida futuro.
Despesa futura = Despesa atual × (1 + taxa_inflação)^n
Saldo mensal = Rendimentos - Despesas ajustadas
Poupança acumulada = Soma dos saldos mensais ao longo de n meses

**Exemplo:**
- Despesas variáveis atuais: 500€/mês
- Taxa de inflação: 2.5%
- Despesas daqui a 12 meses: 500 × (1 + 0.025)^1 = 512,50€

---

### 6. Break-even (Ponto de Equilíbrio)
Mês break-even = Primeiro mês em que:
Poupança Acumulada >= Meta definida pelo aluno

**Exemplo:**
- Meta: 1.000€
- Poupança mensal: 120€
- Break-even = mês 9 (120 × 9 = 1.080€ >= 1.000€)

---

## Parâmetros por Tipo de Simulação

| Parâmetro | Conservadora | Moderada | Agressiva |
|---|---|---|---|
| Taxa de juro poupança (TAN) | 1.5% | 3.0% | 5.0% |
| Taxa de inflação anual | 4.0% | 2.5% | 1.5% |
| Taxa de juro crédito (TAN) | 6.0% | 4.5% | 3.0% |

---

## Exemplos Práticos

### Cenário: Família Silva (Simulação Moderada)
- Rendimento líquido mensal: 2.000€
- Despesas fixas: 800€ (renda 600€ + seguros 200€)
- Despesas variáveis: 600€ (alimentação 300€ + transportes 150€ + lazer 150€)
- Saldo mensal: 600€
- Meta de poupança: 3.000€ em 6 meses

**Resultados:**
- Taxa de esforço: (800 / 2.000) × 100 = 40% ⚠️
- Poupança acumulada em 6 meses: 600 × 6 = 3.600€ ✅
- Break-even: mês 5 (600 × 5 = 3.000€)
- Projeção com inflação (2.5%): despesas variáveis no mês 6 = 607,50€

---

## Notas de Implementação

- Todos os valores monetários devem ser tratados com 2 casas decimais
- As taxas de juro são anuais e devem ser convertidas para mensais na implementação
- O `scenario_id` referencia cenários na `dec_educativa` — a consistência é garantida pelo portal MVC
- Os resultados das simulações são guardados em JSON na tabela `simulationshistory`
- O campo `jsonresults` deve seguir esta estrutura:

```json
{
  "estrategia": "moderada",
  "taxa_esforco": 40.0,
  "poupanca_acumulada": 3600.0,
  "break_even_mes": 5,
  "custo_total_credito": 0,
  "projecao_12_meses": 7200.0,
  "projecao_24_meses": 14400.0,
  "meta_atingida": true
}
```