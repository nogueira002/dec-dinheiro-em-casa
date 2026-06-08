namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorDashboardViewModel
{
    public int TotalClasses { get; set; }
    public int TotalStudents { get; set; }
    public int ActiveChallengesCount { get; set; }
    public int PendingSubmissionsCount { get; set; }
    public IReadOnlyList<DashboardClassViewModel> Classes { get; set; } = [];
    public IReadOnlyList<DashboardSubmissionViewModel> RecentSubmissions { get; set; } = [];
}

public class DashboardClassViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public int ActiveChallengesCount { get; set; }
}

public class DashboardSubmissionViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string ChallengeTitle { get; set; } = string.Empty;
    public string? ClassroomName { get; set; }
    public DateTime SubmittedAtUtc { get; set; }
    public bool IsGraded { get; set; }
}
