using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;

namespace dotnet_mvc.Services.Finance;

public interface IFinancialEntryService
{
    Task<IReadOnlyList<EntryListItemViewModel>> GetScenarioEntriesAsync(int userId, int scenarioId, string? typeFilter, CancellationToken ct = default);
    Task<bool> AddEntryAsync(int userId, int scenarioId, CreateEntryViewModel model, CancellationToken ct = default);
    Task<bool> UpdateEntryAsync(int userId, int entryId, CreateEntryViewModel model, CancellationToken ct = default);
    Task<bool> DeleteEntryAsync(int userId, int entryId, CancellationToken ct = default);
}

public sealed class FinancialEntryService : IFinancialEntryService
{
    private readonly FinancialEntryCRUD _crud = new();

    // Delega em FinancialEntryCRUD → sp_GetScenarioEntries
    public Task<IReadOnlyList<EntryListItemViewModel>> GetScenarioEntriesAsync(
        int userId, int scenarioId, string? typeFilter, CancellationToken ct = default)
        => _crud.ListarAsync(userId, scenarioId, typeFilter, ct);

    // Delega em FinancialEntryCRUD → sp_AddFinancialEntry
    public Task<bool> AddEntryAsync(int userId, int scenarioId, CreateEntryViewModel model, CancellationToken ct = default)
        => _crud.AdicionarAsync(userId, scenarioId, model, ct);

    // Delega em FinancialEntryCRUD → sp_UpdateFinancialEntry
    public Task<bool> UpdateEntryAsync(int userId, int entryId, CreateEntryViewModel model, CancellationToken ct = default)
        => _crud.EditarAsync(userId, entryId, model, ct);

    // Delega em FinancialEntryCRUD → sp_DeleteFinancialEntry
    public Task<bool> DeleteEntryAsync(int userId, int entryId, CancellationToken ct = default)
        => _crud.ApagarAsync(userId, entryId, ct);
}
