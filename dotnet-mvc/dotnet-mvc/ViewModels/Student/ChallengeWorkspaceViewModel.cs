using dotnet_mvc.Services.Finance;

namespace dotnet_mvc.ViewModels.Student;

public class ChallengeWorkspaceViewModel
{
    public StudentChallengeDetailsViewModel Challenge { get; set; } = null!;
    public SimulationViewModel Simulation { get; set; } = new();
    public string ActiveTab { get; set; } = "simulate";
    public StrategyComparisonViewModel? Comparison { get; set; }
}

public class StrategyComparisonViewModel
{
    public SimulationResult? Conservative { get; set; }
    public SimulationResult? Moderate { get; set; }
    public SimulationResult? Aggressive { get; set; }

    // Parâmetros usados na comparação (para re-preencher o form)
    public decimal RendimentoMensal { get; set; }
    public decimal DespesasMensais { get; set; }
    public decimal EncargosFixos { get; set; }
    public decimal CapitalPoupanca { get; set; }
    public decimal Emprestimo { get; set; }
    public int Meses { get; set; } = 24;
    public int? ScenarioId { get; set; }
    public IReadOnlyList<ScenarioListItemViewModel> Scenarios { get; set; } = [];

    public string? ErrorMessage { get; set; }
    public bool HasResults => Conservative is not null || Moderate is not null || Aggressive is not null;
}
