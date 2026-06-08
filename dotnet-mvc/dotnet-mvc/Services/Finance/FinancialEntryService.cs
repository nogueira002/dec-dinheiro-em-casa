using dotnet_mvc.Data;
using dotnet_mvc.Models;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

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
    private readonly AppDbContext _db;

    public FinancialEntryService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<EntryListItemViewModel>> GetScenarioEntriesAsync(
        int userId, int scenarioId, string? typeFilter, CancellationToken ct = default)
    {
        var ownsScenario = await _db.Scenarios
            .AsNoTracking()
            .AnyAsync(s => s.Id == scenarioId && s.OwnerId == userId, ct);

        if (!ownsScenario)
        {
            return [];
        }

        var query = _db.FinancialEntries
            .AsNoTracking()
            .Where(e => e.ScenarioId == scenarioId);

        if (typeFilter == "Income")
        {
            query = query.Where(e => e.EntryType == "Income");
        }
        else if (typeFilter == "Expense")
        {
            query = query.Where(e => e.EntryType == "Expense");
        }

        var rows = await query
            .OrderByDescending(e => e.Year)
            .ThenByDescending(e => e.Month)
            .ThenByDescending(e => e.CreatedAtUtc)
            .Select(e => new { e.Id, e.EntryType, e.Category, e.Amount, e.Month, e.Year, e.IsRecurring })
            .ToListAsync(ct);

        return rows.Select(e => new EntryListItemViewModel
        {
            Id = e.Id,
            IsIncome = e.EntryType == "Income",
            Category = e.Category,
            Amount = e.Amount,
            Month = e.Month,
            MonthName = CreateEntryViewModel.MonthNames[e.Month],
            Year = e.Year,
            IsRecurring = e.IsRecurring
        }).ToList();
    }

    public async Task<bool> AddEntryAsync(int userId, int scenarioId, CreateEntryViewModel model, CancellationToken ct = default)
    {
        var ownsScenario = await _db.Scenarios
            .AnyAsync(s => s.Id == scenarioId && s.OwnerId == userId, ct);

        if (!ownsScenario)
        {
            return false;
        }

        _db.FinancialEntries.Add(new FinancialEntry
        {
            ScenarioId = scenarioId,
            EntryType = model.EntryType == "Income" ? "Income" : "Expense",
            Category = model.Category.Trim(),
            Amount = model.Amount,
            Month = model.Month,
            Year = model.Year,
            IsRecurring = model.IsRecurring,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateEntryAsync(int userId, int entryId, CreateEntryViewModel model, CancellationToken ct = default)
    {
        var entry = await _db.FinancialEntries
            .Include(e => e.Scenario)
            .SingleOrDefaultAsync(e => e.Id == entryId && e.Scenario.OwnerId == userId, ct);

        if (entry is null) return false;

        entry.EntryType  = model.EntryType == "Income" ? "Income" : "Expense";
        entry.Category   = model.Category.Trim();
        entry.Amount     = model.Amount;
        entry.Month      = model.Month;
        entry.Year       = model.Year;
        entry.IsRecurring = model.IsRecurring;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteEntryAsync(int userId, int entryId, CancellationToken ct = default)
    {
        var entry = await _db.FinancialEntries
            .Include(e => e.Scenario)
            .SingleOrDefaultAsync(e => e.Id == entryId && e.Scenario.OwnerId == userId, ct);

        if (entry is null)
        {
            return false;
        }

        _db.FinancialEntries.Remove(entry);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
