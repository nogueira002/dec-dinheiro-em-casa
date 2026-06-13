using System.Data;
using System.Globalization;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;
using MySqlConnector;

namespace dotnet_mvc.Services.Dashboard;

public interface IStudentDashboardService
{
    Task<StudentDashboardViewModel> GetDashboardAsync(int studentId, CancellationToken cancellationToken = default);
}

public sealed class StudentDashboardService : DriverDB, IStudentDashboardService
{
    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");

    private readonly ScenarioCRUD _scenarioCrud = new();

    public async Task<StudentDashboardViewModel> GetDashboardAsync(
        int studentId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        // 1. Cenários (reutiliza sp_GetUserScenarios via ScenarioCRUD)
        var scenarios = await _scenarioCrud.ListarAsync(studentId, cancellationToken);

        // 2. Desafios e submissões via sp_GetStudentChallenges
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetStudentChallenges";
        cmd.Parameters.Add("p_student_id", MySqlDbType.Int32).Value = studentId;
        DataTable dt = await GetSelectAsync(cmd, cancellationToken);

        // 3. Montar lista de desafios
        var challengeResults = new List<StudentDashboardChallengeResultViewModel>(dt.Rows.Count);
        foreach (DataRow row in dt.Rows)
        {
            var deadline     = row["Deadline"] is DBNull ? DateTime.MaxValue : Convert.ToDateTime(row["Deadline"]);
            var hasSubmit    = Convert.ToInt32(row["HasSubmitted"]) > 0;
            var grade        = row["Grade"] is DBNull ? (decimal?)null : Convert.ToDecimal(row["Grade"]);
            var isGraded     = grade.HasValue;
            var submittedAt  = row["SubmittedAtUtc"] is DBNull ? (DateTime?)null : Convert.ToDateTime(row["SubmittedAtUtc"]);
            var status       = ResolveChallengeStatus(deadline, hasSubmit, isGraded, today);

            challengeResults.Add(new StudentDashboardChallengeResultViewModel
            {
                Id            = Convert.ToInt32(row["Id"]),
                Title         = row["Title"].ToString()!,
                ClassroomName = row["ClassroomName"].ToString()!,
                Professor     = row["ProfessorName"].ToString()!,
                Deadline      = deadline == DateTime.MaxValue ? DateTime.MinValue : deadline,
                StatusKey     = status.Key,
                StatusLabel   = status.Label,
                GradeDisplay  = isGraded ? FormatGrade(grade!.Value) : null,
                SubmittedAtUtc = submittedAt
            });
        }

        // 4. Métricas agregadas
        var pendingCount    = challengeResults.Count(x => x.StatusKey == "pending");
        var submittedCount  = challengeResults.Count(x => x.StatusKey == "submitted" || x.StatusKey == "graded");
        var reviewedCount   = challengeResults.Count(x => x.StatusKey == "graded");
        var expiredCount    = challengeResults.Count(x => x.StatusKey == "expired");
        var gradedValues    = challengeResults.Where(x => x.GradeDisplay != null).Select(x => ParseGradeDisplay(x.GradeDisplay!)).ToList();

        var upcomingChallenge = challengeResults.Where(x => x.StatusKey == "pending")
                                                .OrderBy(x => x.Deadline)
                                                .FirstOrDefault();

        var maxBalance      = scenarios.Count == 0 ? 0m : scenarios.Max(x => x.InitialBalance);

        var scenarioComparisons = scenarios
            .OrderByDescending(x => x.InitialBalance)
            .Take(5)
            .Select(x => new StudentDashboardScenarioPointViewModel
            {
                Id                    = x.Id,
                Name                  = x.Name,
                Location              = x.Location,
                ProfileTypeLabel      = x.ProfileTypeLabel,
                InitialBalance        = x.InitialBalance,
                InitialBalanceDisplay = FormatCurrency(x.InitialBalance),
                BalancePercentOfMax   = maxBalance <= 0m ? 0m :
                    Math.Round(x.InitialBalance * 100m / maxBalance, 1, MidpointRounding.AwayFromZero)
            })
            .ToList();

        var recentScenarios = scenarios
            .Take(5)
            .Select(x => new StudentDashboardScenarioSummaryViewModel
            {
                Id                    = x.Id,
                Name                  = x.Name,
                Location              = x.Location,
                ProfileTypeLabel      = x.ProfileTypeLabel,
                InitialBalanceDisplay = FormatCurrency(x.InitialBalance),
                UpdatedAtUtc          = x.UpdatedAtUtc
            })
            .ToList();

        return new StudentDashboardViewModel
        {
            ScenarioCount                  = scenarios.Count,
            AverageInitialBalanceDisplay   = scenarios.Count == 0 ? FormatCurrency(0m) : FormatCurrency(scenarios.Average(x => x.InitialBalance)),
            HighestInitialBalanceDisplay   = maxBalance == 0m ? FormatCurrency(0m) : FormatCurrency(maxBalance),
            HighestInitialBalanceScenarioName = scenarios.OrderByDescending(x => x.InitialBalance).FirstOrDefault()?.Name,
            ChallengeCount                 = challengeResults.Count,
            PendingChallengesCount         = pendingCount,
            SubmittedChallengesCount       = submittedCount,
            ReviewedChallengesCount        = reviewedCount,
            ExpiredChallengesCount         = expiredCount,
            GradedChallengesCount          = gradedValues.Count,
            CompletionRatePercent          = challengeResults.Count == 0 ? 0m :
                Math.Round((decimal)submittedCount * 100m / challengeResults.Count, 1, MidpointRounding.AwayFromZero),
            CompletionRateLabel            = challengeResults.Count == 0 ? "0%" :
                $"{Math.Round((decimal)submittedCount * 100m / challengeResults.Count, 1, MidpointRounding.AwayFromZero).ToString("0.#", PtCulture)}%",
            AverageGradeDisplay            = gradedValues.Count == 0 ? "Sem nota" : FormatGrade(gradedValues.Average()),
            UpcomingDeadlineText           = upcomingChallenge is null ? null :
                $"{upcomingChallenge.Title} · {upcomingChallenge.Deadline.ToString("dd/MM/yyyy", PtCulture)}",
            ScenarioComparisons            = scenarioComparisons,
            RecentScenarios                = recentScenarios,
            ChallengeResults               = challengeResults.Take(6).ToList()
        };
    }

    private static (string Key, string Label) ResolveChallengeStatus(
        DateTime deadline, bool hasSubmission, bool isGraded, DateTime today)
    {
        if (isGraded)   return ("graded",    "Avaliado");
        if (hasSubmission) return ("submitted", "Submetido");
        return deadline < today ? ("expired", "Expirado") : ("pending", "Pendente");
    }

    private static string FormatCurrency(decimal value) =>
        string.Format(PtCulture, "{0:N2} EUR", value);

    private static string FormatGrade(decimal grade) =>
        $"{grade.ToString("0.#", PtCulture)}/20";

    private static decimal ParseGradeDisplay(string gradeDisplay)
    {
        var part = gradeDisplay.Split('/')[0];
        return decimal.TryParse(part, NumberStyles.Any, PtCulture, out var v) ? v : 0m;
    }
}
