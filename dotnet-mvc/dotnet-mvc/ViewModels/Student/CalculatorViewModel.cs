using System.ComponentModel.DataAnnotations;
using dotnet_mvc.Services.Finance;

namespace dotnet_mvc.ViewModels.Student;

public class CalculatorViewModel
{
    public string ActiveTab { get; set; } = "esforco";
    public EsforcoCalcVM    Esforco        { get; set; } = new();
    public CapitalizacaoCalcVM Capitalizacao { get; set; } = new();
    public CreditoCalcVM    Credito        { get; set; } = new();
    public ProjecaoCalcVM   Projecao       { get; set; } = new();
}

public class EsforcoCalcVM
{
    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal EncargosFixos     { get; set; }

    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal RendimentoLiquido { get; set; }

    public TaxaEsforcoResult? Result { get; set; }
    public string? Error { get; set; }
}

public class CapitalizacaoCalcVM
{
    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal Capital { get; set; }

    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true)]
    public decimal TanPercent { get; set; } = 3m;

    [Range(1, 360)]
    public int Meses { get; set; } = 24;

    public CapitalizacaoResult? Result { get; set; }
    public string? Error { get; set; }
}

public class CreditoCalcVM
{
    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal Emprestimo { get; set; }

    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true)]
    public decimal TanPercent { get; set; } = 4.5m;

    [Range(1, 360)]
    public int Meses { get; set; } = 36;

    public CreditoResult? Result { get; set; }
    public string? Error { get; set; }
}

public class ProjecaoCalcVM
{
    [Range(typeof(decimal), "0.01", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal RendimentoMensal { get; set; }

    [Range(typeof(decimal), "0", "9999999", ParseLimitsInInvariantCulture = true)]
    public decimal DespesasMensais  { get; set; }

    [Range(typeof(decimal), "0", "100", ParseLimitsInInvariantCulture = true)]
    public decimal TaxaInflacaoPercent { get; set; } = 2.5m;

    [Range(1, 360)]
    public int Meses { get; set; } = 24;

    public ProjecaoResult? Result { get; set; }
    public string? Error { get; set; }
}
