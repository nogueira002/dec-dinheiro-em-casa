namespace dotnet_mvc.ViewModels.Professor;

public class ClassroomStudentListItemViewModel
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
    public string AssociationLabel { get; set; } = string.Empty;
    public int AvailableChallengesCount { get; set; }
    public int SubmittedChallengesCount { get; set; }
    public int ReviewedChallengesCount { get; set; }
    public int MissingChallengesCount { get; set; }
    public decimal CompletionRatePercent { get; set; }
    public string CompletionRateLabel { get; set; } = "0%";
    public string? AverageGrade { get; set; }
    public string? ChallengeSummary { get; set; }
}
