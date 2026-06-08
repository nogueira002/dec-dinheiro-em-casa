namespace dotnet_mvc.Models;

public class SavingsGoal
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    public string Description { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal SavedAmount { get; set; }
    public int DeadlineMonths { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
