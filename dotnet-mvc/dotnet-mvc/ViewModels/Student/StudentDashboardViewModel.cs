namespace dotnet_mvc.ViewModels.Student;

public class StudentDashboardViewModel
{
    public int ScenarioCount { get; set; }
    public string AverageInitialBalanceDisplay { get; set; } = "0,00 EUR";
    public string HighestInitialBalanceDisplay { get; set; } = "0,00 EUR";
    public string? HighestInitialBalanceScenarioName { get; set; }

    public int ChallengeCount { get; set; }
    public int PendingChallengesCount { get; set; }
    public int SubmittedChallengesCount { get; set; }
    public int ReviewedChallengesCount { get; set; }
    public int ExpiredChallengesCount { get; set; }
    public int GradedChallengesCount { get; set; }
    public decimal CompletionRatePercent { get; set; }
    public string CompletionRateLabel { get; set; } = "0%";
    public string AverageGradeDisplay { get; set; } = "Sem nota";
    public string? UpcomingDeadlineText { get; set; }

    public IReadOnlyList<StudentDashboardScenarioPointViewModel> ScenarioComparisons { get; set; } = [];
    public IReadOnlyList<StudentDashboardScenarioSummaryViewModel> RecentScenarios { get; set; } = [];
    public IReadOnlyList<StudentDashboardChallengeResultViewModel> ChallengeResults { get; set; } = [];
}
