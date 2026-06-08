using dotnet_mvc.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Scenarios;

public interface IScenarioService
{
    Task<IReadOnlyList<ScenarioListItemViewModel>> GetUserScenariosAsync(int userId, CancellationToken cancellationToken = default);
    Task<ScenarioEditorViewModel?> GetScenarioForEditAsync(int userId, int scenarioId, CancellationToken cancellationToken = default);
    Task<int> CreateScenarioAsync(int userId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateScenarioAsync(int userId, int scenarioId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default);
    Task<ScenarioQuickDataDto?> GetScenarioQuickDataAsync(int userId, int scenarioId, CancellationToken cancellationToken = default);
}

public sealed class ScenarioService : IScenarioService
{
    private readonly AppDbContext _dbContext;

    public ScenarioService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ScenarioListItemViewModel>> GetUserScenariosAsync(int userId, CancellationToken cancellationToken = default)
    {
        var scenarios = await _dbContext.Scenarios
            .AsNoTracking()
            .Where(x => x.OwnerId == userId)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Location,
                x.MembersCount,
                x.InitialBalance,
                x.ProfileType,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return scenarios
            .Select(x => new ScenarioListItemViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                MembersCount = x.MembersCount,
                InitialBalance = x.InitialBalance,
                ProfileTypeLabel = x.ProfileType.GetDisplayName(),
                CreatedAtUtc = x.CreatedAtUtc,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToList();
    }

    public async Task<ScenarioEditorViewModel?> GetScenarioForEditAsync(int userId, int scenarioId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Scenarios
            .AsNoTracking()
            .Where(x => x.OwnerId == userId && x.Id == scenarioId)
            .Select(x => new ScenarioEditorViewModel
            {
                Id = x.Id,
                Name = x.Name,
                Location = x.Location,
                MembersCount = x.MembersCount,
                InitialBalance = x.InitialBalance,
                ProfileType = x.ProfileType
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<int> CreateScenarioAsync(int userId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var scenario = new Scenario
        {
            OwnerId = userId,
            Name = model.Name.Trim(),
            Location = model.Location.Trim(),
            MembersCount = model.MembersCount,
            InitialBalance = model.InitialBalance!.Value,
            ProfileType = model.ProfileType,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Scenarios.Add(scenario);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return scenario.Id;
    }

    public async Task<bool> UpdateScenarioAsync(int userId, int scenarioId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.Scenarios
            .SingleOrDefaultAsync(x => x.OwnerId == userId && x.Id == scenarioId, cancellationToken);

        if (scenario is null)
        {
            return false;
        }

        scenario.Name = model.Name.Trim();
        scenario.Location = model.Location.Trim();
        scenario.MembersCount = model.MembersCount;
        scenario.InitialBalance = model.InitialBalance!.Value;
        scenario.ProfileType = model.ProfileType;
        scenario.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ScenarioQuickDataDto?> GetScenarioQuickDataAsync(int userId, int scenarioId, CancellationToken cancellationToken = default)
    {
        var scenario = await _dbContext.Scenarios
            .AsNoTracking()
            .Include(s => s.Entries)
            .Include(s => s.Goals)
            .Where(s => s.Id == scenarioId && s.OwnerId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (scenario is null) return null;

        static decimal MonthlyAvg(IEnumerable<dotnet_mvc.Models.FinancialEntry> entries)
        {
            var groups = entries
                .GroupBy(e => new { e.Month, e.Year })
                .Select(g => g.Sum(e => e.Amount))
                .ToList();
            return groups.Count > 0 ? groups.Average() : 0m;
        }

        // Capital poupado = soma do que já foi reforçado nos objetivos;
        // se não há objetivos, usa o saldo inicial do cenário como fallback.
        var totalSaved = scenario.Goals.Sum(g => g.SavedAmount);
        var capitalPoupanca = totalSaved > 0 ? totalSaved : scenario.InitialBalance;

        return new ScenarioQuickDataDto
        {
            RendimentoMensal = MonthlyAvg(scenario.Entries.Where(e => e.EntryType == "Income")),
            DespesasMensais  = MonthlyAvg(scenario.Entries.Where(e => e.EntryType == "Expense" && !e.IsRecurring)),
            EncargosFixos    = MonthlyAvg(scenario.Entries.Where(e => e.EntryType == "Expense" && e.IsRecurring)),
            CapitalPoupanca  = capitalPoupanca
        };
    }
}
