namespace dotnet_mvc.Models;

public class Scenario
{
    public int Id { get; set; }

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int MembersCount { get; set; }
    public decimal InitialBalance { get; set; }
    public ScenarioProfileType ProfileType { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
