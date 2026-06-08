namespace dotnet_mvc.ViewModels.Student;

public class ScenarioQuickDataDto
{
    public decimal RendimentoMensal { get; set; }
    public decimal DespesasMensais { get; set; }
    public decimal EncargosFixos { get; set; }
    public decimal CapitalPoupanca { get; set; }
}

public class ScenarioListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int MembersCount { get; set; }
    public decimal InitialBalance { get; set; }
    public string ProfileTypeLabel { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
