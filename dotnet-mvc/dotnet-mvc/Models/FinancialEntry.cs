namespace dotnet_mvc.Models;

public class FinancialEntry
{
    public int Id { get; set; }

    public int ScenarioId { get; set; }
    public Scenario Scenario { get; set; } = null!;

    public string EntryType { get; set; } = "Expense"; // "Income" | "Expense"
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public bool IsRecurring { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
