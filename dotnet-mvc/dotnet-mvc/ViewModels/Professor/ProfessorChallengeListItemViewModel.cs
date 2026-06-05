namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorChallengeListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public string StatusKey { get; set; } = "draft";
    public string StatusLabel { get; set; } = "Rascunho";
    public string? ClassroomName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime? Deadline { get; set; }
    public int AudienceCount { get; set; }
    public bool CanAssign { get; set; }
}
