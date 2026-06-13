using System.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;
using MySqlConnector;

namespace dotnet_mvc.Services.Scenarios;

public interface IScenarioService
{
    Task<IReadOnlyList<ScenarioListItemViewModel>> GetUserScenariosAsync(int userId, CancellationToken cancellationToken = default);
    Task<ScenarioEditorViewModel?> GetScenarioForEditAsync(int userId, int scenarioId, CancellationToken cancellationToken = default);
    Task<int> CreateScenarioAsync(int userId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default);
    Task<bool> UpdateScenarioAsync(int userId, int scenarioId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default);
    Task<ScenarioQuickDataDto?> GetScenarioQuickDataAsync(int userId, int scenarioId, CancellationToken cancellationToken = default);
}

public sealed class ScenarioService : DriverDB, IScenarioService
{
    private readonly ScenarioCRUD _crud = new();

    // sp_GetUserScenarios — delega no CRUD legado
    public Task<IReadOnlyList<ScenarioListItemViewModel>> GetUserScenariosAsync(
        int userId, CancellationToken cancellationToken = default)
        => _crud.ListarAsync(userId, cancellationToken);

    public async Task<ScenarioEditorViewModel?> GetScenarioForEditAsync(
        int userId, int scenarioId, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetScenarioById";
        cmd.Parameters.Add("p_scenario_id", MySqlDbType.Int32).Value = scenarioId;
        cmd.Parameters.Add("p_owner_id",    MySqlDbType.Int32).Value = userId;

        DataTable dt = await GetSelectAsync(cmd, cancellationToken);
        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new ScenarioEditorViewModel
        {
            Id             = Convert.ToInt32(row["Id"]),
            Name           = row["Name"].ToString()!,
            Location       = row["Location"].ToString()!,
            MembersCount   = Convert.ToInt32(row["MembersCount"]),
            InitialBalance = Convert.ToDecimal(row["InitialBalance"]),
            ProfileType    = (ScenarioProfileType)Convert.ToInt32(row["ProfileType"])
        };
    }

    public async Task<int> CreateScenarioAsync(
        int userId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_CreateScenario";
        cmd.Parameters.Add("p_owner_id",        MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_name",            MySqlDbType.VarChar, 120).Value = model.Name.Trim();
        cmd.Parameters.Add("p_location",        MySqlDbType.VarChar, 120).Value = model.Location.Trim();
        cmd.Parameters.Add("p_members_count",   MySqlDbType.Int32).Value        = model.MembersCount;
        cmd.Parameters.Add("p_initial_balance", MySqlDbType.Decimal).Value      = model.InitialBalance!.Value;
        cmd.Parameters.Add("p_profile_type",    MySqlDbType.Int32).Value        = (int)model.ProfileType;

        MySqlParameter pId      = new MySqlParameter("p_id",       MySqlDbType.Int32)        { Direction = ParameterDirection.Output };
        MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId); cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return Convert.ToInt32(pId.Value);
    }

    public async Task<bool> UpdateScenarioAsync(
        int userId, int scenarioId, ScenarioEditorViewModel model, CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_UpdateScenario";
        cmd.Parameters.Add("p_scenario_id",     MySqlDbType.Int32).Value        = scenarioId;
        cmd.Parameters.Add("p_owner_id",        MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_name",            MySqlDbType.VarChar, 120).Value = model.Name.Trim();
        cmd.Parameters.Add("p_location",        MySqlDbType.VarChar, 120).Value = model.Location.Trim();
        cmd.Parameters.Add("p_members_count",   MySqlDbType.Int32).Value        = model.MembersCount;
        cmd.Parameters.Add("p_initial_balance", MySqlDbType.Decimal).Value      = model.InitialBalance!.Value;
        cmd.Parameters.Add("p_profile_type",    MySqlDbType.Int32).Value        = (int)model.ProfileType;

        MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        await ExecuteNonQueryAsync(cmd, cancellationToken);
        return Convert.ToInt32(pHaErro.Value) == 0;
    }

    // sp_GetScenarioQuickData — delega no CRUD legado
    public Task<ScenarioQuickDataDto?> GetScenarioQuickDataAsync(
        int userId, int scenarioId, CancellationToken cancellationToken = default)
        => _crud.GetQuickDataAsync(userId, scenarioId, cancellationToken);
}
