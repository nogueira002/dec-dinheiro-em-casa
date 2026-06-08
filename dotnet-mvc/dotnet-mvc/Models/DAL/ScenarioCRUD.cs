using MySqlConnector;
using System.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.ViewModels.Student;

namespace dotnet_mvc.Models.DAL;

public class ScenarioCRUD : DriverDB
{
    // Lista todos os cenários de um utilizador — chama sp_GetUserScenarios
    public async Task<IReadOnlyList<ScenarioListItemViewModel>> ListarAsync(int userId, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetUserScenarios";
        cmd.Parameters.Add("p_user_id", MySqlDbType.Int32).Value = userId;

        DataTable dt = await GetSelectAsync(cmd, ct);
        var lista = new List<ScenarioListItemViewModel>();

        foreach (DataRow row in dt.Rows)
        {
            lista.Add(new ScenarioListItemViewModel
            {
                Id             = Convert.ToInt32(row["Id"]),
                Name           = row["Name"].ToString()!,
                Location       = row["Location"].ToString()!,
                MembersCount   = Convert.ToInt32(row["MembersCount"]),
                InitialBalance = Convert.ToDecimal(row["InitialBalance"]),
                ProfileTypeLabel = ((Models.ScenarioProfileType)Convert.ToInt32(row["ProfileType"])).GetDisplayName(),
                CreatedAtUtc   = Convert.ToDateTime(row["CreatedAtUtc"]),
                UpdatedAtUtc   = Convert.ToDateTime(row["UpdatedAtUtc"])
            });
        }

        return lista;
    }

    // Retorna médias mensais e capital poupado de um cenário — chama sp_GetScenarioQuickData
    public async Task<ScenarioQuickDataDto?> GetQuickDataAsync(int userId, int scenarioId, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetScenarioQuickData";
        cmd.Parameters.Add("p_scenario_id", MySqlDbType.Int32).Value = scenarioId;
        cmd.Parameters.Add("p_owner_id",    MySqlDbType.Int32).Value = userId;

        DataTable dt = await GetSelectAsync(cmd, ct);

        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new ScenarioQuickDataDto
        {
            RendimentoMensal = Convert.ToDecimal(row["rendimento_mensal"]),
            DespesasMensais  = Convert.ToDecimal(row["despesas_mensais"]),
            EncargosFixos    = Convert.ToDecimal(row["encargos_fixos"]),
            CapitalPoupanca  = Convert.ToDecimal(row["capital_poupanca"])
        };
    }
}
