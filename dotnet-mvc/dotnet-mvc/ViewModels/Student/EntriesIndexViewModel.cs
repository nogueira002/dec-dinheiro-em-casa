namespace dotnet_mvc.ViewModels.Student;

public class EntriesIndexViewModel
{
    public int? SelectedScenarioId { get; set; }
    public IReadOnlyList<ScenarioListItemViewModel> AvailableScenarios { get; set; } = [];
    public IReadOnlyList<EntryListItemViewModel> Entries { get; set; } = [];
    public string? TypeFilter { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance => TotalIncome - TotalExpense;
    public CreateEntryViewModel Form { get; set; } = new();
}
