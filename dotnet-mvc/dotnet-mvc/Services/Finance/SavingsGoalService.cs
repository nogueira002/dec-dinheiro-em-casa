using System.Data;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;
using MySqlConnector;

namespace dotnet_mvc.Services.Finance;

public interface ISavingsGoalService
{
    Task<IReadOnlyList<GoalListItemViewModel>> GetScenarioGoalsAsync(int userId, int scenarioId, CancellationToken ct = default);
    Task<bool> CreateGoalAsync(int userId, int scenarioId, CreateObjectiveViewModel model, CancellationToken ct = default);
    Task<bool> AddSavingsAsync(int userId, int goalId, decimal amount, CancellationToken ct = default);
    Task<bool> DeleteGoalAsync(int userId, int goalId, CancellationToken ct = default);
}

public sealed class SavingsGoalService : DriverDB, ISavingsGoalService
{
    public async Task<IReadOnlyList<GoalListItemViewModel>> GetScenarioGoalsAsync(
        int userId, int scenarioId, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetScenarioGoals";
        cmd.Parameters.Add("p_scenario_id", MySqlDbType.Int32).Value = scenarioId;
        cmd.Parameters.Add("p_owner_id",    MySqlDbType.Int32).Value = userId;

        DataTable dt = await GetSelectAsync(cmd, ct);
        var result   = new List<GoalListItemViewModel>(dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            var target  = Convert.ToDecimal(row["TargetAmount"]);
            var saved   = Convert.ToDecimal(row["SavedAmount"]);
            var pct     = target > 0
                ? (int)Math.Min(100, Math.Round(saved / target * 100, MidpointRounding.AwayFromZero))
                : 0;

            result.Add(new GoalListItemViewModel
            {
                Id             = Convert.ToInt32(row["Id"]),
                Description    = row["Description"].ToString()!,
                TargetAmount   = target,
                SavedAmount    = saved,
                DeadlineMonths = Convert.ToInt32(row["DeadlineMonths"]),
                ProgressPercent = pct,
                CreatedAtUtc   = Convert.ToDateTime(row["CreatedAtUtc"])
            });
        }

        return result;
    }

    public async Task<bool> CreateGoalAsync(
        int userId, int scenarioId, CreateObjectiveViewModel model, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_CreateSavingsGoal";
        cmd.Parameters.Add("p_scenario_id",     MySqlDbType.Int32).Value        = scenarioId;
        cmd.Parameters.Add("p_owner_id",        MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_description",     MySqlDbType.VarChar, 200).Value = model.Description.Trim();
        cmd.Parameters.Add("p_target_amount",   MySqlDbType.Decimal).Value      = model.TargetAmount;
        cmd.Parameters.Add("p_deadline_months", MySqlDbType.Int32).Value        = model.DeadlineMonths;

        MySqlParameter pId      = new MySqlParameter("p_id",       MySqlDbType.Int32)        { Direction = ParameterDirection.Output };
        MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pId); cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch { return false; }
    }

    public async Task<bool> AddSavingsAsync(int userId, int goalId, decimal amount, CancellationToken ct = default)
    {
        if (amount <= 0) return false;

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_AddSavingsToGoal";
        cmd.Parameters.Add("p_goal_id",  MySqlDbType.Int32).Value   = goalId;
        cmd.Parameters.Add("p_owner_id", MySqlDbType.Int32).Value   = userId;
        cmd.Parameters.Add("p_amount",   MySqlDbType.Decimal).Value = amount;

        MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch { return false; }
    }

    public async Task<bool> DeleteGoalAsync(int userId, int goalId, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_DeleteSavingsGoal";
        cmd.Parameters.Add("p_goal_id",  MySqlDbType.Int32).Value = goalId;
        cmd.Parameters.Add("p_owner_id", MySqlDbType.Int32).Value = userId;

        MySqlParameter pHaErro  = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg     = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch { return false; }
    }
}
