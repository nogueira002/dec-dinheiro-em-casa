using System.Security.Claims;
using dotnet_mvc.Data;
using dotnet_mvc.Extensions;
using dotnet_mvc.Models;
using dotnet_mvc.Security;
using dotnet_mvc.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Controllers;

public class AuthController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(AppDbContext dbContext, IPasswordHasher<User> passwordHasher)
    {
        _dbContext = dbContext;
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

        // 1. Procurar o utilizador pelo email
        var email = model.Email.Trim().ToLowerInvariant();
        var utilizador = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);

        // 2. Verificar se existe e está ativo
        if (utilizador is null || utilizador.UserStatus != "Active")
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        // 3. Verificar a password
        var resultado = _passwordHasher.VerifyHashedPassword(utilizador, utilizador.PasswordHash, model.Password);
        if (resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        // 4. Criar a sessão (cookie)
        await IniciarSessaoAsync(utilizador, model.RememberMe);

        TempData["StatusType"] = "success";
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

        var email = model.Email.Trim().ToLowerInvariant();

        // Verificar se o email já está registado
        var emailExiste = await _dbContext.Users.AnyAsync(u => u.Email == email);
        if (emailExiste)
        {
            ModelState.AddModelError(nameof(model.Email), "Já existe uma conta com este email.");
            return View(model);
        }

        // Criar o novo utilizador
        var utilizador = new User
        {
            Name = model.Name.Trim(),
            Email = email,
            Role = UserRoles.Normalize(model.Role),
            UserStatus = "Active"
        };
        utilizador.PasswordHash = _passwordHasher.HashPassword(utilizador, model.Password);

        _dbContext.Users.Add(utilizador);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "Não foi possível criar a conta com este email.");
            return View(model);
        }

        await IniciarSessaoAsync(utilizador, rememberMe: false);

        TempData["StatusType"] = "success";
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
        // Terminar a sessão — apaga a cookie do browser
        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);

        TempData["StatusType"] = "info";
        TempData["StatusMessage"] = "Sessão terminada com sucesso.";

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // Cria a cookie de sessão com os dados do utilizador
    private async Task IniciarSessaoAsync(User utilizador, bool rememberMe)
    {
        var agora = DateTime.UtcNow;
        var expira = agora.Add(rememberMe ? AuthConstants.PersistentSessionLifetime : AuthConstants.SessionLifetime);

        // Guardar data do último login
        utilizador.LastLoginAtUtc = agora;
        await _dbContext.SaveChangesAsync();

        // Criar a identidade com os dados do utilizador
        var claims   = AppClaimsFactory.CriarClaims(utilizador);
        var identity  = new ClaimsIdentity(claims, AuthConstants.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Assinar a cookie — o ASP.NET Core trata da encriptação e validação
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

    // Redirecionar para o dashboard correto consoante o role
    private IActionResult RedirecionarParaDashboard(string? returnUrl, string? role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        var rota = UserRoles.GetDefaultRoute(role);
        return RedirectToAction(rota.Action, rota.Controller);
    }
}
