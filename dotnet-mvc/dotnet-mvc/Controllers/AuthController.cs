using System.Data;
using System.Security.Claims;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.Security;
using dotnet_mvc.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace dotnet_mvc.Controllers;

public class AuthController : Controller
{
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(returnUrl, User.FindFirstValue(ClaimTypes.Role));

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(model.ReturnUrl, User.FindFirstValue(ClaimTypes.Role));

        if (!ModelState.IsValid)
            return View(model);

        var email      = model.Email.Trim().ToLowerInvariant();
        var utilizador = await GetUserByEmailAsync(email);

        if (utilizador is null || utilizador.UserStatus != "Active")
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        var resultado = _passwordHasher.VerifyHashedPassword(utilizador, utilizador.PasswordHash, model.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        await IniciarSessaoAsync(utilizador, model.RememberMe);

        TempData["StatusType"]    = "success";
        TempData["StatusMessage"] = "Sessão iniciada com sucesso.";

        return RedirecionarParaDashboard(model.ReturnUrl, utilizador.Role);
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(null, User.FindFirstValue(ClaimTypes.Role));

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(null, User.FindFirstValue(ClaimTypes.Role));

        if (!ModelState.IsValid)
            return View(model);

        var email     = model.Email.Trim().ToLowerInvariant();
        var role      = UserRoles.Normalize(model.Role);
        var utilizador = new User
        {
            Name       = model.Name.Trim(),
            Email      = email,
            Role       = role,
            UserStatus = "Active"
        };
        utilizador.PasswordHash = _passwordHasher.HashPassword(utilizador, model.Password);

        var (newId, erro, mensagem) = await CreateUserAsync(utilizador);
        if (erro)
        {
            ModelState.AddModelError(nameof(model.Email), mensagem ?? "Não foi possível criar a conta com este email.");
            return View(model);
        }

        utilizador.Id = newId;
        await IniciarSessaoAsync(utilizador, rememberMe: false);

        TempData["StatusType"]    = "success";
        TempData["StatusMessage"] = "Conta criada com sucesso.";

        return RedirecionarParaDashboard(null, utilizador.Role);
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(null, User.FindFirstValue(ClaimTypes.Role));

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirecionarParaDashboard(null, User.FindFirstValue(ClaimTypes.Role));

        if (!ModelState.IsValid)
            return View(model);

        return View(new ForgotPasswordViewModel { Email = model.Email, IsConfirmationDisplayed = true });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);

        TempData["StatusType"]    = "info";
        TempData["StatusMessage"] = "Sessão terminada com sucesso.";

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ── Helpers de dados (ADO.NET direto) ────────────────────────────────────

    private static async Task<User?> GetUserByEmailAsync(string email)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText  = "sp_GetUserByEmail";
        cmd.CommandType  = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_email", email);
        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new User
        {
            Id           = reader.GetInt32("Id"),
            Name         = reader.GetString("Name"),
            Email        = reader.GetString("Email"),
            PasswordHash = reader.GetString("PasswordHash"),
            Role         = reader.GetString("Role"),
            UserStatus   = reader.GetString("UserStatus"),
            Gender       = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetString("Gender"),
            LastLoginAtUtc = reader.IsDBNull(reader.GetOrdinal("LastLoginAtUtc")) ? null : reader.GetDateTime("LastLoginAtUtc")
        };
    }

    private static async Task<(int Id, bool Erro, string? Mensagem)> CreateUserAsync(User u)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_CreateUser";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_name",          u.Name);
        cmd.Parameters.AddWithValue("p_email",         u.Email);
        cmd.Parameters.AddWithValue("p_password_hash", u.PasswordHash);
        cmd.Parameters.AddWithValue("p_role",          u.Role);
        cmd.Parameters.AddWithValue("p_gender",        (object?)u.Gender ?? DBNull.Value);

        var pId      = cmd.Parameters.Add("p_id",       MySqlDbType.Int32);       pId.Direction      = ParameterDirection.Output;
        var pHaErro  = cmd.Parameters.Add("p_ha_erro",  MySqlDbType.Byte);        pHaErro.Direction  = ParameterDirection.Output;
        var pMsg     = cmd.Parameters.Add("p_mensagem", MySqlDbType.VarChar, 500); pMsg.Direction     = ParameterDirection.Output;

        await cmd.ExecuteNonQueryAsync();
        bool erro = Convert.ToInt32(pHaErro.Value) != 0;
        return (Convert.ToInt32(pId.Value), erro, pMsg.Value?.ToString());
    }

    private static async Task UpdateLastLoginAsync(int userId)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "sp_UpdateUserLastLogin";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("p_user_id", userId);
        await cmd.ExecuteNonQueryAsync();
    }

    // ── Sessão ───────────────────────────────────────────────────────────────

    private async Task IniciarSessaoAsync(User utilizador, bool rememberMe)
    {
        var agora  = DateTime.UtcNow;
        var expira = agora.Add(rememberMe ? AuthConstants.PersistentSessionLifetime : AuthConstants.SessionLifetime);

        await UpdateLastLoginAsync(utilizador.Id);

        var claims    = AppClaimsFactory.CriarClaims(utilizador);
        var identity  = new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AuthConstants.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                IssuedUtc    = agora,
                ExpiresUtc   = expira
            });
    }

    private IActionResult RedirecionarParaDashboard(string? returnUrl, string? role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        var rota = UserRoles.GetDefaultRoute(role);
        return RedirectToAction(rota.Action, rota.Controller);
    }
}
