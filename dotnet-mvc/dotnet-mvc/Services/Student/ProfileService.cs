using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Student;
using Microsoft.AspNetCore.Identity;
using MySqlConnector;
using System.Data;

namespace dotnet_mvc.Services.Student;

public interface IProfileService
{
    Task<ProfileViewModel?> GetProfileAsync(int userId, CancellationToken ct = default);
    Task<(bool Sucesso, string? Mensagem)> UpdateProfileAsync(int userId, ProfileViewModel model, CancellationToken ct = default);
}

public class ProfileService : DriverDB, IProfileService
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public ProfileService(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    public async Task<ProfileViewModel?> GetProfileAsync(int userId, CancellationToken ct = default)
    {
        var cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_GetUserProfile";
        cmd.Parameters.Add("p_user_id", MySqlDbType.Int32).Value = userId;

        DataTable dt = await GetSelectAsync(cmd, ct);

        if (dt.Rows.Count == 0) return null;

        DataRow row = dt.Rows[0];
        return new ProfileViewModel
        {
            Name      = row["Name"].ToString()!,
            Email     = row["Email"].ToString()!,
            Gender    = row["Gender"] == DBNull.Value ? "" : row["Gender"].ToString()!,
            BirthDate = row["BirthDate"] == DBNull.Value ? null : DateOnly.FromDateTime(Convert.ToDateTime(row["BirthDate"]))
        };
    }

    public async Task<(bool Sucesso, string? Mensagem)> UpdateProfileAsync(int userId, ProfileViewModel model, CancellationToken ct = default)
    {
        var passwordHash = string.Empty;
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var tempUser = new User { Id = userId };
            passwordHash = _passwordHasher.HashPassword(tempUser, model.NewPassword);
        }

        var cmd = new MySqlCommand();
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.CommandText = "sp_UpdateUserProfile";
        cmd.Parameters.Add("p_user_id",       MySqlDbType.Int32).Value        = userId;
        cmd.Parameters.Add("p_name",          MySqlDbType.VarChar, 120).Value = model.Name;
        cmd.Parameters.Add("p_email",         MySqlDbType.VarChar, 256).Value = model.Email.Trim().ToLowerInvariant();
        cmd.Parameters.Add("p_gender",        MySqlDbType.VarChar,  10).Value = model.Gender;
        cmd.Parameters.Add("p_birthdate",     MySqlDbType.Date).Value         = (object?)model.BirthDate ?? DBNull.Value;
        cmd.Parameters.Add("p_password_hash", MySqlDbType.VarChar, 512).Value = passwordHash;

        var pHaErro   = new MySqlParameter("p_ha_erro",  MySqlDbType.Byte)        { Direction = ParameterDirection.Output };
        var pMensagem = new MySqlParameter("p_mensagem", MySqlDbType.VarChar, 500) { Direction = ParameterDirection.Output };
        cmd.Parameters.Add(pHaErro);
        cmd.Parameters.Add(pMensagem);

        try
        {
            await ExecuteNonQueryAsync(cmd, ct);
            bool sucesso = Convert.ToInt32(pHaErro.Value) == 0;
            string? mensagem = sucesso ? null : pMensagem.Value?.ToString();
            return (sucesso, mensagem);
        }
        catch (Exception)
        {
            return (false, "Erro inesperado ao guardar o perfil.");
        }
    }
}
