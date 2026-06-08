# DEC — Dinheiro Em Casa · Django REST API

**Base URL:** `/api/`  
**Content-Type:** `application/json`  
**Autenticação:** Nenhuma (endpoints públicos)  
**Documentação interativa:** `/api/docs/` (Swagger UI)

---

## Índice

1. [Endpoints de Estado](#1-endpoints-de-estado)
2. [Calculadores Financeiros](#2-calculadores-financeiros)
3. [Lançamentos (Entries)](#3-lançamentos-entries)
4. [Objetivos (Objectives)](#4-objetivos-objectives)
5. [Histórico de Simulações](#5-histórico-de-simulações)
6. [Simulação Completa](#6-simulação-completa)
7. [Modelos de Base de Dados](#7-modelos-de-base-de-dados)
8. [Estratégias de Simulação](#8-estratégias-de-simulação)
9. [Códigos de Erro](#9-códigos-de-erro)

---

## 1. Endpoints de Estado

### `GET /api/`
Verifica se a API está operacional.

**Resposta `200 OK`**
```json
{
  "mensagem": "Bem vindo à API do DEC - Dinheiro Em Casa!",
  "linguagem": "Django REST Framework",
  "status": "Ok Running!",
  "timestamp": "2024-01-15T10:30:45.123456"
}
```

---

## 2. Calculadores Financeiros

### `POST /api/taxa-esforco/`
Calcula a taxa de esforço (rácio encargos fixos / rendimento líquido).

**Request**
```json
{
  "encargos_fixos": 500.00,
  "rendimento_liquido": 2000.00
}
```

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `encargos_fixos` | float | Sim | Total de encargos fixos mensais (€) |
| `rendimento_liquido` | float | Sim | Rendimento líquido mensal (€) — deve ser > 0 |

**Resposta `200 OK`**
```json
{
  "taxa_esforco": 25.0,
  "classificacao": "Confortável"
}
```

| Taxa (%) | Classificação |
|---|---|
| < 30 | Confortável |
| 30 – 35 | Equilibrado |
| 35 – 50 | Preocupante |
| > 50 | Risco Elevado |

---

### `POST /api/capitalizacao/`
Calcula a capitalização composta de um capital ao longo do tempo.

**Fórmula:** `Montante = Capital × (1 + TAN/12) ^ meses`

**Request**
```json
{
  "capital": 5000.00,
  "tan": 0.03,
  "meses": 24
}
```

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `capital` | float | Sim | Capital inicial (€) — deve ser > 0 |
| `tan` | float | Sim | Taxa anual nominal (ex: 0.03 = 3%) — deve ser >= 0 |
| `meses` | int | Sim | Número de meses — deve ser > 0 |

**Resposta `200 OK`**
```json
{
  "montante_final": 5386.40,
  "juros_gerados": 386.40
}
```

---

### `POST /api/simular-credito/`
Simula um empréstimo usando o método de amortização Price (prestação constante).

**Fórmula:** `Prestação = Empréstimo × [i / (1 − (1+i)^−n)]`  onde `i = TAN/12`

**Request**
```json
{
  "emprestimo": 50000.00,
  "tan": 0.04,
  "meses": 240
}
```

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `emprestimo` | float | Sim | Montante do empréstimo (€) — deve ser > 0 |
| `tan` | float | Sim | Taxa anual nominal — deve ser >= 0 |
| `meses` | int | Sim | Prazo em meses — deve ser > 0 |

**Resposta `200 OK`**
```json
{
  "prestacao_mensal": 231.16,
  "custo_total": 55478.40,
  "juros_totais": 5478.40
}
```

---

### `POST /api/projecao-poupanca/`
Projeta a poupança ao longo do tempo, ajustando as despesas à inflação em cada mês.

**Algoritmo:** `despesa_ajustada_mes = despesas × (1 + taxa_inflacao)^mes`

**Request**
```json
{
  "rendimento_mensal": 2500.00,
  "despesas_mensais": 1800.00,
  "taxa_inflacao": 0.02,
  "meses": 12
}
```

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `rendimento_mensal` | float | Sim | Rendimento mensal fixo (€) — deve ser > 0 |
| `despesas_mensais` | float | Sim | Despesas mensais base (€) — deve ser >= 0 |
| `taxa_inflacao` | float | Sim | Taxa de inflação mensal (ex: 0.02 = 2%) |
| `meses` | int | Sim | Número de meses a projetar — deve ser > 0 |

**Resposta `200 OK`**
```json
{
  "poupanca_acumulada": 7263.45,
  "saldo_medio_mensal": 605.29,
  "break_even_mes": 1
}
```

| Campo | Descrição |
|---|---|
| `poupanca_acumulada` | Total poupado no período (€) |
| `saldo_medio_mensal` | Média do saldo mensal (€) |
| `break_even_mes` | Primeiro mês em que a poupança acumulada fica positiva (`null` se nunca) |

---

## 3. Lançamentos (Entries)

### `GET /api/entries/?scenario_id={id}`
Lista todos os lançamentos de um cenário.

**Query Parameter obrigatório:** `scenario_id` (int)

**Resposta `200 OK`**
```json
[
  {
    "id": 1,
    "scenario_id": 1,
    "type": "rendimento",
    "category": "Salário",
    "amount": 2500.00,
    "month": 1,
    "year": 2024,
    "recurrence": "mensal"
  }
]
```

---

### `POST /api/entries/`
Cria um novo lançamento financeiro.

**Request**
```json
{
  "scenario_id": 1,
  "type": "rendimento",
  "category": "Salário",
  "amount": 2500.00,
  "month": 1,
  "year": 2024,
  "recurrence": "mensal"
}
```

| Campo | Tipo | Valores aceites | Descrição |
|---|---|---|---|
| `scenario_id` | int | Qualquer int | Referência ao cenário |
| `type` | string | `rendimento`, `despesa` | Tipo do lançamento |
| `category` | string | Texto livre (max 100) | Categoria |
| `amount` | float | Decimal(10,2) | Valor em € |
| `month` | int | 1–12 | Mês |
| `year` | int | Qualquer int | Ano |
| `recurrence` | string | `mensal`, `anual`, `unica` | Periodicidade |

**Resposta `201 Created`** — igual ao request com `id` gerado automaticamente.

---

## 4. Objetivos (Objectives)

### `GET /api/objectives/?scenario_id={id}`
Lista todos os objetivos de um cenário.

**Query Parameter obrigatório:** `scenario_id` (int)

**Resposta `200 OK`**
```json
[
  {
    "id": 1,
    "scenario_id": 1,
    "description": "Fundo de emergência",
    "targetvalue": 15000.00,
    "termmonths": 18,
    "current_value": 2000.00
  }
]
```

---

### `POST /api/objectives/`
Cria um novo objetivo financeiro.

**Request**
```json
{
  "scenario_id": 1,
  "description": "Fundo de emergência",
  "targetvalue": 15000.00,
  "termmonths": 18,
  "current_value": 0.00
}
```

| Campo | Tipo | Descrição |
|---|---|---|
| `scenario_id` | int | Referência ao cenário |
| `description` | string | Descrição do objetivo (max 255) |
| `targetvalue` | float | Valor alvo (€) |
| `termmonths` | int | Prazo em meses |
| `current_value` | float | Valor atual já acumulado (€), default 0 |

**Resposta `201 Created`** — igual ao request com `id` gerado automaticamente.

---

## 5. Histórico de Simulações

### `GET /api/simulation-history/?scenario_id={id}`
Recupera o histórico de simulações de um cenário.

**Query Parameter obrigatório:** `scenario_id` (int)

**Resposta `200 OK`**
```json
[
  {
    "id": 1,
    "scenario_id": 1,
    "executiondate": "2024-01-15T10:30:45.123456Z",
    "simulation_type": "moderate",
    "jsonresults": { ... }
  }
]
```

---

### `POST /api/simulation-history/`
Guarda manualmente um resultado de simulação.

> **Nota:** O endpoint `POST /api/simulate/` guarda automaticamente o histórico — este endpoint é para casos específicos.

**Request**
```json
{
  "scenario_id": 1,
  "simulation_type": "moderate",
  "jsonresults": { ... }
}
```

| Campo | Tipo | Valores aceites | Descrição |
|---|---|---|---|
| `scenario_id` | int | Qualquer int | Referência ao cenário |
| `simulation_type` | string | `conservative`, `moderate`, `aggressive` | Estratégia usada |
| `jsonresults` | JSON object | Qualquer objeto JSON | Resultado completo da simulação |

**Resposta `201 Created`** — inclui `executiondate` gerado automaticamente.

---

## 6. Simulação Completa

### `POST /api/simulate/`
Executa uma simulação financeira completa com a estratégia selecionada. **Guarda automaticamente o resultado no histórico.**

**Request**
```json
{
  "strategy": "moderate",
  "scenario_id": 1,
  "rendimento_mensal": 2500.00,
  "despesas_mensais": 1800.00,
  "encargos_fixos": 400.00,
  "capital_poupanca": 5000.00,
  "emprestimo": 30000.00,
  "meses": 24
}
```

| Campo | Tipo | Obrigatório | Default | Descrição |
|---|---|---|---|---|
| `strategy` | string | Não | `moderate` | `conservative`, `moderate`, `aggressive` |
| `scenario_id` | int | Não | `0` | ID do cenário |
| `rendimento_mensal` | float | Não | `0` | Rendimento mensal (€) |
| `despesas_mensais` | float | Não | `0` | Despesas mensais (€) |
| `encargos_fixos` | float | Não | `0` | Encargos fixos mensais (€) |
| `capital_poupanca` | float | Não | `0` | Capital para capitalização (€) — se 0, omitido |
| `emprestimo` | float | Não | `0` | Valor do empréstimo (€) — se 0, omitido |
| `meses` | int | Não | `12` | Horizonte temporal em meses |

**Resposta `200 OK`**
```json
{
  "strategy": "moderate",
  "taxa_esforco": {
    "taxa_esforco": 16.0,
    "classificacao": "Confortável"
  },
  "capitalizacao": {
    "montante_final": 5309.14,
    "juros_gerados": 309.14
  },
  "credito": {
    "prestacao_mensal": 1399.19,
    "custo_total": 33580.56,
    "juros_totais": 3580.56
  },
  "projecao": {
    "poupanca_acumulada": 14988.23,
    "saldo_medio_mensal": 624.51,
    "break_even_mes": 1
  }
}
```

> `capitalizacao` e `credito` aparecem como `null` se os campos correspondentes forem 0.

---

## 7. Modelos de Base de Dados

### Entry — tabela `entries`
| Campo | Tipo SQL | Descrição |
|---|---|---|
| `id` | INT PK AUTO | Identificador único |
| `scenario_id` | INT | Referência ao cenário (portal .NET) |
| `type` | VARCHAR(20) | `rendimento` ou `despesa` |
| `category` | VARCHAR(100) | Categoria do lançamento |
| `amount` | DECIMAL(10,2) | Valor em € |
| `month` | INT | Mês (1–12) |
| `year` | INT | Ano |
| `recurrence` | VARCHAR(20) | `mensal`, `anual`, `unica` |

### Objective — tabela `objectives`
| Campo | Tipo SQL | Descrição |
|---|---|---|
| `id` | INT PK AUTO | Identificador único |
| `scenario_id` | INT | Referência ao cenário |
| `description` | VARCHAR(255) | Descrição do objetivo |
| `targetvalue` | DECIMAL(10,2) | Valor alvo (€) |
| `termmonths` | INT | Prazo em meses |
| `current_value` | DECIMAL(10,2) | Valor acumulado atual (€), default 0 |

### SimulationHistory — tabela `simulationshistory`
| Campo | Tipo SQL | Descrição |
|---|---|---|
| `id` | INT PK AUTO | Identificador único |
| `scenario_id` | INT | Referência ao cenário |
| `executiondate` | DATETIME | Data/hora de execução (auto) |
| `simulation_type` | VARCHAR(20) | `conservative`, `moderate`, `aggressive` |
| `jsonresults` | JSON | Resultado completo da simulação |

---

## 8. Estratégias de Simulação

| Estratégia | Juro poupança | Inflação mensal | Juro crédito |
|---|---|---|---|
| `conservative` | 1.5% | 4.0% | 6.0% |
| `moderate` (default) | 3.0% | 2.5% | 4.5% |
| `aggressive` | 5.0% | 1.5% | 3.0% |

---

## 9. Códigos de Erro

| Código | Situação |
|---|---|
| `200 OK` | Sucesso (GET ou cálculo) |
| `201 Created` | Recurso criado com sucesso |
| `400 Bad Request` | Input inválido ou regra de negócio violada |
| `500 Internal Server Error` | Erro inesperado no servidor |

**Formato de erro de negócio:**
```json
{
  "erro": "Mensagem descritiva em português"
}
```

**Formato de erro de validação (serializer):**
```json
{
  "nome_do_campo": ["Mensagem de erro do campo"]
}
```

**Erros de negócio comuns:**

| Mensagem | Causa |
|---|---|
| `O rendimento líquido tem que ser maior que zero.` | `rendimento_liquido <= 0` em `/taxa-esforco/` |
| `O capital deve ser maior que zero.` | `capital <= 0` em `/capitalizacao/` |
| `A taxa de juro não pode ser negativa.` | `tan < 0` |
| `O número de meses deve ser maior que zero.` | `meses <= 0` |
| `O emprestimo deve ser maior que zero.` | `emprestimo <= 0` em `/simular-credito/` |
| `O rendimento mensal deve ser maior que zero.` | `rendimento_mensal <= 0` em `/projecao-poupanca/` |
| `As despesas mensais não podem ser negativas.` | `despesas_mensais < 0` |
| `scenario_id é obrigatório` | Query param ausente em GET de entries/objectives/history |
