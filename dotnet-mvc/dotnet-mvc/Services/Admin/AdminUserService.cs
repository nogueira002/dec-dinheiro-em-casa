using System.Data;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.Security;
using dotnet_mvc.ViewModels.Admin;
using MySqlConnector;

namespace dotnet_mvc.Services.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(
        string? search, string? role, string? status,
        CancellationToken cancellationToken = default);

    Task<(bool Success, bool IsNowActive, string UserName)> ToggleUserStatusAsync(
        int userId, int adminId,
        CancellationToken cancellationToken = default);

    Task<(bool Success, string UserName, string NewRole)> ChangeUserRoleAsync(
        int userId, string newRole, int adminId,
        CancellationToken cancellationToken = default);

    Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default);
}

public sealed class AdminUserService : DriverDB, IAdminUserService
{
    public async Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync(
        string? search, string? role, string? status,
        CancellationToken cancellationToken = default)
    {
        var normalizedRole   = !string.IsNullOrWhiteSpace(role)   && UserRoles.IsSupported(role)  ? role.Trim()   : "";
        var normalizedStatus = status switch { "active" => "Active", "inactive" => "Inactive", _ => "" };

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetUsers";
        cmd.Parameters.Add("p_search", MySqlDbType.VarChar, 256).Value = search?.Trim() ?? "";
        cmd.Parameters.Add("p_role",   MySqlDbType.VarChar, 32).Value  = normalizedRole;
        cmd.Parameters.Add("p_status", MySqlDbType.VarChar, 32).Value  = normalizedStatus;

        DataTable dt     = await GetSelectAsync(cmd, cancellationToken);
        var result       = new List<UserListItemViewModel>(dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            var r = row["Role"].ToString() ?? "";
            result.Add(new UserListItemViewModel
            {
                Id            = Convert.ToInt32(row["Id"]),
                Name          = row["Name"].ToString()!,
                Email         = row["Email"].ToString()!,
                Role          = r,
                RoleLabel     = r == UserRoles.Professor ? "Professor"
                              : r == UserRoles.Admin     ? "Administrador"
                              :                            "Aluno",
                IsActive      = row["UserStatus"].ToString() == "Active",
                LastLoginAtUtc = row["LastLoginAtUtc"] is DBNull ? null : Convert.ToDateTime(row["LastLoginAtUtc"])
            });
        }

        return result;
    }

    public async Task<(bool Success, bool IsNowActive, string UserName)> ToggleUserStatusAsync(
        int userId, int adminId, CancellationToken cancellationToken = default)
    {
        if (userId == adminId) return (false, true, string.Empty);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_ToggleUserStatus";
        cmd.Parameters.Add("p_user_id",    MySqlDbType.Int32).Value = userId;

        MySqlParameter pStatus = new MySqlParameter("p_new_status", MySqlDbType.VarChar, 32) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pStatus);

        try
        {
            await ExecuteNonQueryAsync(cmd, cancellationToken);
            var newStatus = pStatus.Value?.ToString() ?? "";
            if (string.IsNullOrEmpty(newStatus)) return (false, false, string.Empty);

            // Fetch name for the feedback message
            var name = await GetUserNameAsync(userId, cancellationToken);
            return (true, newStatus == "Active", name);
        }
        catch { return (false, false, string.Empty); }
    }

    public async Task<(bool Success, string UserName, string NewRole)> ChangeUserRoleAsync(
        int userId, string newRole, int adminId, CancellationToken cancellationToken = default)
    {
        if (!UserRoles.IsSupported(newRole) || userId == adminId)
            return (false, string.Empty, string.Empty);

        var normalized = UserRoles.Normalize(newRole);

        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_EditUserRole";
        cmd.Parameters.Add("p_user_id",  MySqlDbType.Int32).Value       = userId;
        cmd.Parameters.Add("p_new_role", MySqlDbType.VarChar, 32).Value = normalized;

        MySqlParameter pHaErro = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)         { Direction = ParameterDirection.Output };
        MySqlParameter pMsg    = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro); cmd.Parameters.Add(pMsg);

        try
        {
            await ExecuteNonQueryAsync(cmd, cancellationToken);
            if (Convert.ToInt32(pHaErro.Value) != 0) return (false, string.Empty, string.Empty);

            var name = await GetUserNameAsync(userId, cancellationToken);
            return (true, name, normalized);
        }
        catch { return (false, string.Empty, string.Empty); }
    }

    public async Task<AdminDashboardViewModel> GetAdminDashboardAsync(CancellationToken cancellationToken = default)
    {
        MySqlCommand cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetAdminDashboard";

        DataTable dt = await GetSelectAsync(cmd, cancellationToken);
        if (dt.Rows.Count == 0) return new AdminDashboardViewModel();

        DataRow row = dt.Rows[0];
        return new AdminDashboardViewModel
        {
            TotalUsers       = Convert.ToInt32(row["TotalUsers"]),
            StudentCount     = Convert.ToInt32(row["TotalStudents"]),
            ProfessorCount   = Convert.ToInt32(row["TotalProfessors"]),
            AdminCount       = Convert.ToInt32(row["TotalAdmins"]),
            ActiveCount      = Convert.ToInt32(row["ActiveUsers"]),
            TotalClassrooms  = Convert.ToInt32(row["TotalClassrooms"]),
            TotalChallenges  = Convert.ToInt32(row["TotalChallenges"]),
            TotalSubmissions = Convert.ToInt32(row["TotalSubmissions"]),
            PendingSubmissions = Convert.ToInt32(row["PendingReviews"])
        };
    }

    private async Task<string> GetUserNameAsync(int userId, CancellationToken ct)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Name FROM Users WHERE Id = @id LIMIT 1";
        cmd.Parameters.AddWithValue("@id", userId);
        return (await cmd.ExecuteScalarAsync(ct))?.ToString() ?? string.Empty;
    }
}
