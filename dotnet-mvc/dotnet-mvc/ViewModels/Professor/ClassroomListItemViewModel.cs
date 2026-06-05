namespace dotnet_mvc.ViewModels.Professor;

public class ClassroomListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccessCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ClassroomStudentCount { get; set; }
    public int DirectChallengeParticipantCount { get; set; }
    public int ActiveChallengesCount { get; set; }
    public int TotalChallengesCount { get; set; }
    public int ExpectedSubmissionsCount { get; set; }
    public int SubmittedChallengesCount { get; set; }
    public int ReviewedChallengesCount { get; set; }
    public decimal CompletionRatePercent { get; set; }
    public string CompletionRateLabel { get; set; } = "0%";
    public string? AverageGrade { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<ClassroomChallengeListItemViewModel> Challenges { get; set; } = [];
    public IReadOnlyList<ClassroomStudentListItemViewModel> Students { get; set; } = [];
}
