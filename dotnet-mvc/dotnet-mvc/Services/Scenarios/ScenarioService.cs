using dotnet_mvc.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
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
    private readonly ScenarioCRUD _crud = new();

    public ScenarioService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Delega em ScenarioCRUD → sp_GetUserScenarios
    public Task<IReadOnlyList<ScenarioListItemViewModel>> GetUserScenariosAsync(int userId, CancellationToken cancellationToken = default)
        => _crud.ListarAsync(userId, cancellationToken);

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

        if (scenario is null) return false;

        scenario.Name = model.Name.Trim();
        scenario.Location = model.Location.Trim();
        scenario.MembersCount = model.MembersCount;
        scenario.InitialBalance = model.InitialBalance!.Value;
        scenario.ProfileType = model.ProfileType;
        scenario.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    // Delega em ScenarioCRUD → sp_GetScenarioQuickData
    public Task<ScenarioQuickDataDto?> GetScenarioQuickDataAsync(int userId, int scenarioId, CancellationToken cancellationToken = default)
        => _crud.GetQuickDataAsync(userId, scenarioId, cancellationToken);
}
