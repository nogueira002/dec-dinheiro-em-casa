using System.Globalization;
using dotnet_mvc.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Dashboard;

public interface IStudentDashboardService
{
    Task<StudentDashboardViewModel> GetDashboardAsync(int studentId, CancellationToken cancellationToken = default);
}

public sealed class StudentDashboardService : IStudentDashboardService
{
    private static readonly CultureInfo PtCulture = CultureInfo.GetCultureInfo("pt-PT");

    private readonly AppDbContext _dbContext;

    public StudentDashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentDashboardViewModel> GetDashboardAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var scenarios = await _dbContext.Scenarios
            .AsNoTracking()
            .Where(x => x.OwnerId == studentId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Location,
                x.ProfileType,
                x.InitialBalance,
                x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var challenges = await _dbContext.Challenges
            .AsNoTracking()
            .Where(x => x.PublishedAtUtc != null
                && x.ClassroomId.HasValue
                && x.Deadline.HasValue
                && x.Classroom != null
                && x.Classroom.IsActive
                && (x.Classroom.Memberships.Any(membership => membership.StudentId == studentId)
                    || x.Enrollments.Any(enrollment => enrollment.StudentId == studentId)))
            .OrderBy(x => x.Deadline)
            .ThenBy(x => x.Title)
            .Select(x => new
            {
                x.Id,
                x.Title,
                Professor = x.CreatedByUser.Name,
                ClassroomName = x.Classroom!.Name,
                Deadline = x.Deadline!.Value,
                Submission = x.Submissions
                    .Where(submission => submission.StudentId == studentId)
                    .Select(submission => new
                    {
                        submission.SubmittedAtUtc,
                        submission.Grade
                    })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var maxBalance = scenarios.Count == 0 ? 0m : scenarios.Max(x => x.InitialBalance);
        var scenarioComparisons = scenarios
            .OrderByDescending(x => x.InitialBalance)
            .Take(5)
            .Select(x => new StudentDashboardScenarioPointViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                ProfileTypeLabel = x.ProfileType.GetDisplayName(),
                InitialBalance = x.InitialBalance,
                InitialBalanceDisplay = FormatCurrency(x.InitialBalance),
                BalancePercentOfMax = maxBalance <= 0m ? 0m : Math.Round(x.InitialBalance * 100m / maxBalance, 1, MidpointRounding.AwayFromZero)
            })
            .ToList();

        var recentScenarios = scenarios
            .Take(5)
            .Select(x => new StudentDashboardScenarioSummaryViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                ProfileTypeLabel = x.ProfileType.GetDisplayName(),
                InitialBalanceDisplay = FormatCurrency(x.InitialBalance),
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToList();

        var challengeResults = challenges
            .Select(x =>
            {
                var status = ResolveChallengeStatus(x.Deadline, x.Submission is not null, x.Submission?.Grade.HasValue == true, today);

                return new StudentDashboardChallengeResultViewModel
                {
                    Id = x.Id,
                    Title = x.Title,
                    ClassroomName = x.ClassroomName,
                    Professor = x.Professor,
                    Deadline = x.Deadline,
                    StatusKey = status.Key,
                    StatusLabel = status.Label,
                    GradeDisplay = x.Submission?.Grade.HasValue == true ? FormatGrade(x.Submission.Grade.Value) : null,
                    SubmittedAtUtc = x.Submission?.SubmittedAtUtc
                };
            })
            .ToList();

        var pendingChallengesCount = challengeResults.Count(x => x.StatusKey == "pending");
        var submittedChallengesCount = challengeResults.Count(x => x.StatusKey == "submitted" || x.StatusKey == "graded");
        var reviewedChallengesCount = challengeResults.Count(x => x.StatusKey == "graded");
        var expiredChallengesCount = challengeResults.Count(x => x.StatusKey == "expired");
        var gradedValues = challenges
            .Where(x => x.Submission?.Grade.HasValue == true)
            .Select(x => x.Submission!.Grade!.Value)
            .ToList();
        var highestScenario = scenarios
            .OrderByDescending(x => x.InitialBalance)
            .FirstOrDefault();
        var upcomingChallenge = challengeResults
            .Where(x => x.StatusKey == "pending")
            .OrderBy(x => x.Deadline)
            .FirstOrDefault();

        return new StudentDashboardViewModel
        {
            ScenarioCount = scenarios.Count,
            AverageInitialBalanceDisplay = scenarios.Count == 0
                ? FormatCurrency(0m)
                : FormatCurrency(scenarios.Average(x => x.InitialBalance)),
            HighestInitialBalanceDisplay = highestScenario is null
                ? FormatCurrency(0m)
                : FormatCurrency(highestScenario.InitialBalance),
            HighestInitialBalanceScenarioName = highestScenario?.Name,
            ChallengeCount = challengeResults.Count,
            PendingChallengesCount = pendingChallengesCount,
            SubmittedChallengesCount = submittedChallengesCount,
            ReviewedChallengesCount = reviewedChallengesCount,
            ExpiredChallengesCount = expiredChallengesCount,
            GradedChallengesCount = gradedValues.Count,
            CompletionRatePercent = challengeResults.Count == 0
                ? 0m
                : Math.Round((decimal)submittedChallengesCount * 100m / challengeResults.Count, 1, MidpointRounding.AwayFromZero),
            CompletionRateLabel = challengeResults.Count == 0
                ? "0%"
                : $"{Math.Round((decimal)submittedChallengesCount * 100m / challengeResults.Count, 1, MidpointRounding.AwayFromZero).ToString("0.#", PtCulture)}%",
            AverageGradeDisplay = gradedValues.Count == 0
                ? "Sem nota"
                : FormatGrade(gradedValues.Average()),
            UpcomingDeadlineText = upcomingChallenge is null
                ? null
                : $"{upcomingChallenge.Title} · {upcomingChallenge.Deadline.ToString("dd/MM/yyyy", PtCulture)}",
            ScenarioComparisons = scenarioComparisons,
            RecentScenarios = recentScenarios,
            ChallengeResults = challengeResults.Take(6).ToList()
        };
    }

    private static (string Key, string Label) ResolveChallengeStatus(DateTime deadline, bool hasSubmission, bool isGraded, DateTime today)
    {
        if (isGraded)
        {
            return ("graded", "Avaliado");
        }

        if (hasSubmission)
        {
            return ("submitted", "Submetido");
        }

        return deadline < today
            ? ("expired", "Expirado")
            : ("pending", "Pendente");
    }

    private static string FormatCurrency(decimal value)
    {
        return string.Format(PtCulture, "{0:N2} EUR", value);
    }

    private static string FormatGrade(decimal grade)
    {
        return $"{grade.ToString("0.#", PtCulture)}/20";
    }
}
