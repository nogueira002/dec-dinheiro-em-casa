namespace dotnet_mvc.ViewModels.Student;

public class EntryListItemViewModel
{
    public int Id { get; set; }
    public bool IsIncome { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Year { get; set; }
    public bool IsRecurring { get; set; }
}
