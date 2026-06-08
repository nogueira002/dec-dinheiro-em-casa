namespace dotnet_mvc.ViewModels.Student;

public class GoalListItemViewModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal SavedAmount { get; set; }
    public int DeadlineMonths { get; set; }
    public int ProgressPercent { get; set; }
    public bool IsComplete => ProgressPercent >= 100;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime Deadline => CreatedAtUtc.AddMonths(DeadlineMonths);
}
