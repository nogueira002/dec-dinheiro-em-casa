namespace dotnet_mvc.ViewModels.Student;

public class StudentDashboardChallengeResultViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string StatusKey { get; set; } = "pending";
    public string StatusLabel { get; set; } = "Pendente";
    public string? GradeDisplay { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
}
