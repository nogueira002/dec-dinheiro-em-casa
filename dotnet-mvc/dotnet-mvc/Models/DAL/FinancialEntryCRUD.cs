using MySqlConnector;
using System.Data;
using dotnet_mvc.ViewModels.Student;

namespace dotnet_mvc.Models.DAL;

public class FinancialEntryCRUD : DriverDB
{
    // Lista lançamentos de um cenário com filtro opcional — chama sp_GetScenarioEntries
    public async Task<IReadOnlyList<EntryListItemViewModel>> ListarAsync(
        int userId, int scenarioId, string? typeFilter, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetScenarioEntries";
        cmd.Parameters.Add("p_scenario_id", MySqlDbType.Int32).Value      = scenarioId;
        cmd.Parameters.Add("p_owner_id",    MySqlDbType.Int32).Value      = userId;
        cmd.Parameters.Add("p_type_filter", MySqlDbType.VarChar, 16).Value = typeFilter ?? "";

        DataTable dt = await GetSelectAsync(cmd, ct);
        var lista = new List<EntryListItemViewModel>();

        foreach (DataRow row in dt.Rows)
        {
            int month = Convert.ToInt32(row["Month"]);
            lista.Add(new EntryListItemViewModel
            {
                Id         = Convert.ToInt32(row["Id"]),
                IsIncome   = row["EntryType"].ToString() == "Income",
                Category   = row["Category"].ToString()!,
                Amount     = Convert.ToDecimal(row["Amount"]),
                Month      = month,
                MonthName  = CreateEntryViewModel.MonthNames[month],
                Year       = Convert.ToInt32(row["Year"]),
                IsRecurring = Convert.ToBoolean(row["IsRecurring"])
            });
        }

        return lista;
    }

    // Adiciona um lançamento — chama sp_AddFinancialEntry
    // Output: p_ha_erro (0=ok, 1=erro), p_mensagem (descrição do erro)
    public async Task<bool> AdicionarAsync(
        int userId, int scenarioId, CreateEntryViewModel model, CancellationToken ct = default)
    {
        string entryType = model.EntryType == "Income" ? "Income" : "Expense";

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_AddFinancialEntry";
        cmd.Parameters.Add("p_scenario_id",  MySqlDbType.Int32).Value        = scenarioId;
        cmd.Parameters.Add("p_owner_id",     MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_entry_type",   MySqlDbType.VarChar, 16).Value  = entryType;
        cmd.Parameters.Add("p_category",     MySqlDbType.VarChar, 255).Value = model.Category.Trim();
        cmd.Parameters.Add("p_amount",       MySqlDbType.Decimal).Value      = model.Amount;
        cmd.Parameters.Add("p_month",        MySqlDbType.Int32).Value        = model.Month;
        cmd.Parameters.Add("p_year",         MySqlDbType.Int32).Value        = model.Year;
        cmd.Parameters.Add("p_is_recurring", MySqlDbType.Byte).Value         = model.IsRecurring ? 1 : 0;

        MySqlParameter pHaErro   = new MySqlParameter("p_ha_erro",   MySqlDbType.Byte)       { Direction = ParameterDirection.Output };
        MySqlParameter pMensagem = new MySqlParameter("p_mensagem",  MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro);
        cmd.Parameters.Add(pMensagem);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Edita um lançamento — chama sp_UpdateFinancialEntry
    // Output: p_ha_erro (0=ok, 1=erro), p_mensagem (descrição do erro)
    public async Task<bool> EditarAsync(
        int userId, int entryId, CreateEntryViewModel model, CancellationToken ct = default)
    {
        string entryType = model.EntryType == "Income" ? "Income" : "Expense";

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_UpdateFinancialEntry";
        cmd.Parameters.Add("p_entry_id",     MySqlDbType.Int32).Value        = entryId;
        cmd.Parameters.Add("p_owner_id",     MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_entry_type",   MySqlDbType.VarChar, 16).Value  = entryType;
        cmd.Parameters.Add("p_category",     MySqlDbType.VarChar, 255).Value = model.Category.Trim();
        cmd.Parameters.Add("p_amount",       MySqlDbType.Decimal).Value      = model.Amount;
        cmd.Parameters.Add("p_month",        MySqlDbType.Int32).Value        = model.Month;
        cmd.Parameters.Add("p_year",         MySqlDbType.Int32).Value        = model.Year;
        cmd.Parameters.Add("p_is_recurring", MySqlDbType.Byte).Value         = model.IsRecurring ? 1 : 0;

        MySqlParameter pHaErro   = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)        { Direction = ParameterDirection.Output };
        MySqlParameter pMensagem = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro);
        cmd.Parameters.Add(pMensagem);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Apaga um lançamento — chama sp_DeleteFinancialEntry
    // Output: p_ha_erro (0=ok, 1=erro), p_mensagem (descrição do erro)
    public async Task<bool> ApagarAsync(int userId, int entryId, CancellationToken ct = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_DeleteFinancialEntry";
        cmd.Parameters.Add("p_entry_id",  MySqlDbType.Int32).Value = entryId;
        cmd.Parameters.Add("p_owner_id",  MySqlDbType.Int32).Value = userId;

        MySqlParameter pHaErro   = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)        { Direction = ParameterDirection.Output };
        MySqlParameter pMensagem = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro);
        cmd.Parameters.Add(pMensagem);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            return Convert.ToInt32(pHaErro.Value) == 0;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
