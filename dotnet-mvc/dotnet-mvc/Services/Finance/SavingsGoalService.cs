using dotnet_mvc.Data;
using dotnet_mvc.Models;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Finance;

public interface ISavingsGoalService
{
    Task<IReadOnlyList<GoalListItemViewModel>> GetScenarioGoalsAsync(int userId, int scenarioId, CancellationToken ct = default);
    Task<bool> CreateGoalAsync(int userId, int scenarioId, CreateObjectiveViewModel model, CancellationToken ct = default);
    Task<bool> AddSavingsAsync(int userId, int goalId, decimal amount, CancellationToken ct = default);
    Task<bool> DeleteGoalAsync(int userId, int goalId, CancellationToken ct = default);
}

public sealed class SavingsGoalService : ISavingsGoalService
{
    private readonly AppDbContext _db;

    public SavingsGoalService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<GoalListItemViewModel>> GetScenarioGoalsAsync(
        int userId, int scenarioId, CancellationToken ct = default)
    {
        var ownsScenario = await _db.Scenarios
            .AsNoTracking()
            .AnyAsync(s => s.Id == scenarioId && s.OwnerId == userId, ct);

        if (!ownsScenario)
        {
            return [];
        }

        var rows = await _db.SavingsGoals
            .AsNoTracking()
            .Where(g => g.ScenarioId == scenarioId)
            .OrderBy(g => g.CreatedAtUtc)
            .Select(g => new { g.Id, g.Description, g.TargetAmount, g.SavedAmount, g.DeadlineMonths, g.CreatedAtUtc })
            .ToListAsync(ct);

        return rows.Select(g =>
        {
            var pct = g.TargetAmount > 0
                ? (int)Math.Min(100, Math.Round(g.SavedAmount / g.TargetAmount * 100, MidpointRounding.AwayFromZero))
                : 0;
            return new GoalListItemViewModel
            {
                Id = g.Id,
                Description = g.Description,
                TargetAmount = g.TargetAmount,
                SavedAmount = g.SavedAmount,
                DeadlineMonths = g.DeadlineMonths,
                ProgressPercent = pct,
                CreatedAtUtc = g.CreatedAtUtc
            };
        }).ToList();
    }

    public async Task<bool> CreateGoalAsync(int userId, int scenarioId, CreateObjectiveViewModel model, CancellationToken ct = default)
    {
        var ownsScenario = await _db.Scenarios
            .AnyAsync(s => s.Id == scenarioId && s.OwnerId == userId, ct);

        if (!ownsScenario)
        {
            return false;
        }

        _db.SavingsGoals.Add(new SavingsGoal
        {
            ScenarioId = scenarioId,
            Description = model.Description.Trim(),
            TargetAmount = model.TargetAmount,
            SavedAmount = 0,
            DeadlineMonths = model.DeadlineMonths,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AddSavingsAsync(int userId, int goalId, decimal amount, CancellationToken ct = default)
    {
        var goal = await _db.SavingsGoals
            .Include(g => g.Scenario)
            .SingleOrDefaultAsync(g => g.Id == goalId && g.Scenario.OwnerId == userId, ct);

        if (goal is null || amount <= 0)
        {
            return false;
        }

        goal.SavedAmount = Math.Min(goal.SavedAmount + amount, goal.TargetAmount);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteGoalAsync(int userId, int goalId, CancellationToken ct = default)
    {
        var goal = await _db.SavingsGoals
            .Include(g => g.Scenario)
            .SingleOrDefaultAsync(g => g.Id == goalId && g.Scenario.OwnerId == userId, ct);

        if (goal is null)
        {
            return false;
        }

        _db.SavingsGoals.Remove(goal);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
