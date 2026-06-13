using System.Data;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;
using MySqlConnector;

namespace dotnet_mvc.Services.Finance;

public interface ISimulationHistoryService
{
    Task SaveAsync(int userId, SimulationRequest request, SimulationResult result, CancellationToken ct = default);
    Task<IReadOnlyList<SimulationHistoryItemViewModel>> GetUserHistoryAsync(int userId, int limit = 10, CancellationToken ct = default);
}

public sealed class SimulationHistoryService : DriverDB, ISimulationHistoryService
{
    public async Task SaveAsync(
        int userId, SimulationRequest request, SimulationResult result, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_SaveSimulation";
        cmd.Parameters.Add("p_user_id",            MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_strategy",           MySqlDbType.VarChar, 32).Value  = request.Strategy ?? "";
        cmd.Parameters.Add("p_rendimento_mensal",  MySqlDbType.Decimal).Value      = request.RendimentoMensal;
        cmd.Parameters.Add("p_despesas_mensais",   MySqlDbType.Decimal).Value      = request.DespesasMensais;
        cmd.Parameters.Add("p_meses",              MySqlDbType.Int32).Value        = request.Meses;
        cmd.Parameters.Add("p_taxa_esforco",       MySqlDbType.Decimal).Value      = (object?)result.TaxaEsforco?.TaxaEsforco ?? DBNull.Value;
        cmd.Parameters.Add("p_taxa_esforco_class", MySqlDbType.VarChar, 80).Value  = (object?)result.TaxaEsforco?.Classificacao ?? DBNull.Value;
        cmd.Parameters.Add("p_montante_final",     MySqlDbType.Decimal).Value      = (object?)result.Capitalizacao?.MontanteFinal ?? DBNull.Value;
        cmd.Parameters.Add("p_poupanca_acumulada", MySqlDbType.Decimal).Value      = (object?)result.Projecao?.PoupancaAcumulada ?? DBNull.Value;

        MySqlParameter pId = new MySqlParameter("p_id", MySqlDbType.Int32) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId);

        try { await ExecuteNonQueryAsync(cmd, ct); }
        catch { /* histórico é opcional — não propaga erro */ }
    }

    public async Task<IReadOnlyList<SimulationHistoryItemViewModel>> GetUserHistoryAsync(
        int userId, int limit = 10, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetSimulationHistory";
        cmd.Parameters.Add("p_user_id", MySqlDbType.Int32).Value = userId;
        cmd.Parameters.Add("p_limit",   MySqlDbType.Int32).Value = limit;

        DataTable dt   = await GetSelectAsync(cmd, ct);
        var result     = new List<SimulationHistoryItemViewModel>(dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            var strategy = row["Strategy"].ToString() ?? "";
            result.Add(new SimulationHistoryItemViewModel
            {
                Id                      = Convert.ToInt32(row["Id"]),
                Strategy                = strategy,
                StrategyLabel           = strategy switch
                {
                    "conservative" => "Conservadora",
                    "aggressive"   => "Agressiva",
                    _              => "Moderada"
                },
                RendimentoMensal        = row["RendimentoMensal"]       is DBNull ? 0m  : Convert.ToDecimal(row["RendimentoMensal"]),
                DespesasMensais         = row["DespesasMensais"]        is DBNull ? 0m  : Convert.ToDecimal(row["DespesasMensais"]),
                Meses                   = row["Meses"]                  is DBNull ? 0   : Convert.ToInt32(row["Meses"]),
                TaxaEsforco             = row["TaxaEsforco"]            is DBNull ? null : Convert.ToDecimal(row["TaxaEsforco"]),
                TaxaEsforcoClassificacao = row["TaxaEsforcoClassificacao"] is DBNull ? null : row["TaxaEsforcoClassificacao"].ToString(),
                MontanteFinal           = row["MontanteFinal"]          is DBNull ? null : Convert.ToDecimal(row["MontanteFinal"]),
                PoupancaAcumulada       = row["PoupancaAcumulada"]      is DBNull ? null : Convert.ToDecimal(row["PoupancaAcumulada"]),
                CreatedAtUtc            = Convert.ToDateTime(row["CreatedAtUtc"])
            });
        }

        return result;
    }
}
