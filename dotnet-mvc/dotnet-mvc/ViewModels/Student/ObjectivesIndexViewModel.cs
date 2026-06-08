namespace dotnet_mvc.ViewModels.Student;

public class ObjectivesIndexViewModel
{
    public int? SelectedScenarioId { get; set; }
    public IReadOnlyList<ScenarioListItemViewModel> AvailableScenarios { get; set; } = [];
    public IReadOnlyList<GoalListItemViewModel> Goals { get; set; } = [];
    public CreateObjectiveViewModel Form { get; set; } = new();
}
