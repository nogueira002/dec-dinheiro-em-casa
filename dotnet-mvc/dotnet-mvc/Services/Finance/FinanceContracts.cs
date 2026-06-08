using System.Text.Json.Serialization;

namespace dotnet_mvc.Services.Finance;

// ── Calculadoras individuais ──────────────────────────────────────────────────

public sealed class CalcEsforcoRequest
{
    [JsonPropertyName("encargos_fixos")]   public decimal EncargosFixos    { get; set; }
    [JsonPropertyName("rendimento_liquido")] public decimal RendimentoLiquido { get; set; }
}

public sealed class CalcCapitalizacaoRequest
{
    [JsonPropertyName("capital")] public decimal Capital { get; set; }
    [JsonPropertyName("tan")]     public decimal Tan     { get; set; }
    [JsonPropertyName("meses")]   public int     Meses   { get; set; }
}

public sealed class CalcCreditoRequest
{
    [JsonPropertyName("emprestimo")] public decimal Emprestimo { get; set; }
    [JsonPropertyName("tan")]        public decimal Tan        { get; set; }
    [JsonPropertyName("meses")]      public int     Meses      { get; set; }
}

public sealed class CalcProjecaoRequest
{
    [JsonPropertyName("rendimento_mensal")] public decimal RendimentoMensal { get; set; }
    [JsonPropertyName("despesas_mensais")]  public decimal DespesasMensais  { get; set; }
    [JsonPropertyName("taxa_inflacao")]     public decimal TaxaInflacao     { get; set; }
    [JsonPropertyName("meses")]            public int     Meses            { get; set; }
}

// Pedido enviado para POST /api/simulate/ na API Django.
// Os nomes em snake_case correspondem ao SimulateSerializer do microserviço.
public sealed class SimulationRequest
{
    [JsonPropertyName("strategy")]
    public string Strategy { get; set; } = "moderate";

    [JsonPropertyName("scenario_id")]
    public int ScenarioId { get; set; }

    [JsonPropertyName("rendimento_mensal")]
    public decimal RendimentoMensal { get; set; }

    [JsonPropertyName("despesas_mensais")]
    public decimal DespesasMensais { get; set; }

    [JsonPropertyName("encargos_fixos")]
    public decimal EncargosFixos { get; set; }

    [JsonPropertyName("capital_poupanca")]
    public decimal CapitalPoupanca { get; set; }

    [JsonPropertyName("emprestimo")]
    public decimal Emprestimo { get; set; }

    [JsonPropertyName("meses")]
    public int Meses { get; set; }
}

// Resposta devolvida pela estratégia (conservative/moderate/aggressive) da API Django.
public sealed class SimulationResult
{
    [JsonPropertyName("strategy")]
    public string Strategy { get; set; } = string.Empty;

    [JsonPropertyName("taxa_esforco")]
    public TaxaEsforcoResult? TaxaEsforco { get; set; }

    // capitalizacao e credito vêm a null quando não há capital/empréstimo a simular.
    [JsonPropertyName("capitalizacao")]
    public CapitalizacaoResult? Capitalizacao { get; set; }

    [JsonPropertyName("credito")]
    public CreditoResult? Credito { get; set; }

    [JsonPropertyName("projecao")]
    public ProjecaoResult? Projecao { get; set; }
}

public sealed class TaxaEsforcoResult
{
    [JsonPropertyName("taxa_esforco")]
    public decimal TaxaEsforco { get; set; }

    [JsonPropertyName("classificacao")]
    public string Classificacao { get; set; } = string.Empty;
}

public sealed class CapitalizacaoResult
{
    [JsonPropertyName("montante_final")]
    public decimal MontanteFinal { get; set; }

    [JsonPropertyName("juros_gerados")]
    public decimal JurosGerados { get; set; }
}

public sealed class CreditoResult
{
    [JsonPropertyName("prestacao_mensal")]
    public decimal PrestacaoMensal { get; set; }

    [JsonPropertyName("custo_total")]
    public decimal CustoTotal { get; set; }

    [JsonPropertyName("juros_totais")]
    public decimal JurosTotais { get; set; }
}

public sealed class ProjecaoResult
{
    [JsonPropertyName("poupanca_acumulada")]
    public decimal PoupancaAcumulada { get; set; }

    [JsonPropertyName("saldo_medio_mensal")]
    public decimal SaldoMedioMensal { get; set; }

    // null quando a poupança acumulada nunca fica positiva no período.
    [JsonPropertyName("break_even_mes")]
    public int? BreakEvenMes { get; set; }
}
