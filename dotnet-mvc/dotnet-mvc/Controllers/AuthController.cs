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
    private readonly ISessionTokenService _sessionTokenService;

    public AuthController(
        AppDbContext dbContext,
        IPasswordHasher<User> passwordHasher,
        ISessionTokenService sessionTokenService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _sessionTokenService = sessionTokenService;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(returnUrl, User.FindFirstValue(ClaimTypes.Role));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(model.ReturnUrl, User.FindFirstValue(ClaimTypes.Role));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = NormalizeEmail(model.Email);
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user is null ||
            !string.Equals(user.UserStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email ou password inválidos.");
            return View(model);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
        }

        await SignInUserAsync(user, model.RememberMe);

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Sessão iniciada com sucesso.";

        return RedirectToSafeLocalUrl(model.ReturnUrl, user.Role);
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(null, User.FindFirstValue(ClaimTypes.Role));
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(null, User.FindFirstValue(ClaimTypes.Role));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = NormalizeEmail(model.Email);
        var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (emailExists)
        {
            ModelState.AddModelError(nameof(model.Email), "Já existe uma conta com este email.");
            return View(model);
        }

        var user = new User
        {
            Name = model.Name.Trim(),
            Email = normalizedEmail,
            Role = UserRoles.Normalize(model.Role),
            UserStatus = "Active"
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(nameof(model.Email), "Não foi possível criar a conta com este email.");
            return View(model);
        }

        await SignInUserAsync(user, rememberMe: false);

        TempData["StatusType"] = "success";
        TempData["StatusMessage"] = "Conta criada com sucesso.";

        return RedirectToSafeLocalUrl(null, user.Role);
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(null, User.FindFirstValue(ClaimTypes.Role));
        }

        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToSafeLocalUrl(null, User.FindFirstValue(ClaimTypes.Role));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        return View(new ForgotPasswordViewModel
        {
            Email = model.Email,
            IsConfirmationDisplayed = true
        });
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.GetUserId();
        if (userId.HasValue)
        {
            var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId.Value);
            if (user is not null)
            {
                user.SessionTokenHash = null;
                user.SessionExpiresAtUtc = null;
                await _dbContext.SaveChangesAsync();
            }
        }

        await HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);

        TempData["StatusType"] = "info";
        TempData["StatusMessage"] = "Sessão terminada com sucesso.";

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUserAsync(User user, bool rememberMe)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.Add(rememberMe
            ? AuthConstants.PersistentSessionLifetime
            : AuthConstants.SessionLifetime);

        var sessionToken = _sessionTokenService.GenerateToken();
        user.SessionTokenHash = _sessionTokenService.HashToken(sessionToken);
        user.SessionExpiresAtUtc = expiresAt;
        user.LastLoginAtUtc = now;

        await _dbContext.SaveChangesAsync();

        var identity = new ClaimsIdentity(
            AppClaimsFactory.CreateClaims(user, sessionToken),
            AuthConstants.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AuthConstants.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                AllowRefresh = true,
                IssuedUtc = now,
                ExpiresUtc = expiresAt
            });
    }

    private IActionResult RedirectToSafeLocalUrl(string? returnUrl, string? role)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        var route = UserRoles.GetDefaultRoute(role);
        return RedirectToAction(route.Action, route.Controller);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
