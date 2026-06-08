using System.ComponentModel.DataAnnotations;
using dotnet_mvc.Services.Finance;

namespace dotnet_mvc.ViewModels.Student;

public class SimulationViewModel
{
    [Display(Name = "Cenário")]
    public int? ScenarioId { get; set; }

    [Required(ErrorMessage = "Escolhe uma estratégia.")]
    [RegularExpression("conservative|moderate|aggressive", ErrorMessage = "Estratégia inválida.")]
    [Display(Name = "Estratégia")]
    public string Strategy { get; set; } = "moderate";

    [Range(3, 120, ErrorMessage = "O período deve estar entre 3 e 120 meses.")]
    [Display(Name = "Período (meses)")]
    public int Meses { get; set; } = 24;

    [Range(typeof(decimal), "0.01", "999999999.99", ErrorMessage = "O rendimento mensal tem de ser maior que zero.", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Rendimento mensal (€)")]
    public decimal RendimentoMensal { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Valor inválido.", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Despesas mensais (€)")]
    public decimal DespesasMensais { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Valor inválido.", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Encargos fixos (€)")]
    public decimal EncargosFixos { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Valor inválido.", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Capital para poupança (€)")]
    public decimal CapitalPoupanca { get; set; }

    [Range(typeof(decimal), "0", "999999999.99", ErrorMessage = "Valor inválido.", ParseLimitsInInvariantCulture = true)]
    [Display(Name = "Empréstimo (€)")]
    public decimal Emprestimo { get; set; }

    public IReadOnlyList<ScenarioListItemViewModel> Scenarios { get; set; } = [];
    public IReadOnlyList<SimulationHistoryItemViewModel> History { get; set; } = [];

    public SimulationResult? Result { get; set; }
    public string? ErrorMessage { get; set; }

    public bool HasResult => Result is not null;
}
