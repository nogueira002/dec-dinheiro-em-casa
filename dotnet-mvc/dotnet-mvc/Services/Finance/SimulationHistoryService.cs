using dotnet_mvc.Data;
using dotnet_mvc.Models;
using dotnet_mvc.ViewModels.Student;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Finance;

public interface ISimulationHistoryService
{
    Task SaveAsync(int userId, SimulationRequest request, SimulationResult result, CancellationToken ct = default);
    Task<IReadOnlyList<SimulationHistoryItemViewModel>> GetUserHistoryAsync(int userId, int limit = 10, CancellationToken ct = default);
}

public sealed class SimulationHistoryService : ISimulationHistoryService
{
    private readonly AppDbContext _db;

    public SimulationHistoryService(AppDbContext db) => _db = db;

    public async Task SaveAsync(int userId, SimulationRequest request, SimulationResult result, CancellationToken ct = default)
    {
        _db.SimulationHistories.Add(new SimulationHistory
        {
            UserId = userId,
            Strategy = request.Strategy,
            RendimentoMensal = request.RendimentoMensal,
            DespesasMensais = request.DespesasMensais,
            Meses = request.Meses,
            TaxaEsforco = result.TaxaEsforco?.TaxaEsforco,
            TaxaEsforcoClassificacao = result.TaxaEsforco?.Classificacao,
            MontanteFinal = result.Capitalizacao?.MontanteFinal,
            PoupancaAcumulada = result.Projecao?.PoupancaAcumulada,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<SimulationHistoryItemViewModel>> GetUserHistoryAsync(
        int userId, int limit = 10, CancellationToken ct = default)
    {
        var rows = await _db.SimulationHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAtUtc)
            .Take(limit)
            .Select(h => new
            {
                h.Id, h.Strategy, h.RendimentoMensal, h.DespesasMensais,
                h.Meses, h.TaxaEsforco, h.TaxaEsforcoClassificacao,
                h.MontanteFinal, h.PoupancaAcumulada, h.CreatedAtUtc
            })
            .ToListAsync(ct);

        return rows.Select(h => new SimulationHistoryItemViewModel
        {
            Id = h.Id,
            Strategy = h.Strategy,
            StrategyLabel = h.Strategy switch
            {
                "conservative" => "Conservadora",
                "aggressive"   => "Agressiva",
                _              => "Moderada"
            },
            RendimentoMensal = h.RendimentoMensal,
            DespesasMensais = h.DespesasMensais,
            Meses = h.Meses,
            TaxaEsforco = h.TaxaEsforco,
            TaxaEsforcoClassificacao = h.TaxaEsforcoClassificacao,
            MontanteFinal = h.MontanteFinal,
            PoupancaAcumulada = h.PoupancaAcumulada,
            CreatedAtUtc = h.CreatedAtUtc
        }).ToList();
    }
}
