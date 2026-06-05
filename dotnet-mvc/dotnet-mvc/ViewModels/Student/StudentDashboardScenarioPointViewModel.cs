namespace dotnet_mvc.ViewModels.Student;

public class StudentDashboardScenarioPointViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string ProfileTypeLabel { get; set; } = string.Empty;
    public string InitialBalanceDisplay { get; set; } = string.Empty;
    public decimal BalancePercentOfMax { get; set; }
}
