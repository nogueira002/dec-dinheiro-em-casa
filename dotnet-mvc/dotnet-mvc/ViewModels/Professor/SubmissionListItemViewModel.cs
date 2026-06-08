namespace dotnet_mvc.ViewModels.Professor;

public class SubmissionListItemViewModel
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public string ChallengeTitle { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsGraded { get; set; }
    public string? GradeDisplay { get; set; }
}
