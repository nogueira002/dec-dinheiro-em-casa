namespace dotnet_mvc.ViewModels.Professor;

public class SubmissionsIndexViewModel
{
    public IReadOnlyList<SubmissionListItemViewModel> Submissions { get; set; } = [];
    public IReadOnlyList<ClassroomOptionViewModel> AvailableClasses { get; set; } = [];
    public IReadOnlyList<ChallengeOptionViewModel> AvailableChallenges { get; set; } = [];
    public int? SelectedChallengeId { get; set; }
    public int? SelectedClassroomId { get; set; }
    public string? StatusFilter { get; set; }
    public int PendingCount { get; set; }
    public int GradedCount { get; set; }
}
