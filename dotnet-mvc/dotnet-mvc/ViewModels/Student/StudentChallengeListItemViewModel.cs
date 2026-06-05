namespace dotnet_mvc.ViewModels.Student;

public class StudentChallengeListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string StatusKey { get; set; } = "pending";
    public string StatusLabel { get; set; } = "Pendente";
    public string? Grade { get; set; }
    public bool HasSubmission { get; set; }
    public bool CanEditSubmission { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}
