using System.Security.Claims;
using dotnet_mvc.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Security;

public sealed class CookieSessionValidator : CookieAuthenticationEvents
{
    private readonly AppDbContext _dbContext;
    private readonly ISessionTokenService _sessionTokenService;

    public CookieSessionValidator(AppDbContext dbContext, ISessionTokenService sessionTokenService)
    {
        _dbContext = dbContext;
        _sessionTokenService = sessionTokenService;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var sessionToken = context.Principal?.FindFirstValue(AuthConstants.SessionTokenClaimType);

        if (!int.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(sessionToken))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        var user = await _dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == userId);
        if (user is null ||
            !string.Equals(user.UserStatus, "Active", StringComparison.OrdinalIgnoreCase) ||
            user.SessionExpiresAtUtc is null ||
            user.SessionExpiresAtUtc <= DateTime.UtcNow ||
            string.IsNullOrWhiteSpace(user.SessionTokenHash) ||
            !_sessionTokenService.Matches(sessionToken, user.SessionTokenHash))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        var currentName = context.Principal?.Identity?.Name;
        var currentRole = context.Principal?.FindFirstValue(ClaimTypes.Role);
        var currentEmail = context.Principal?.FindFirstValue(ClaimTypes.Email);

        if (!string.Equals(currentName, user.Name, StringComparison.Ordinal) ||
            !string.Equals(currentRole, user.Role, StringComparison.Ordinal) ||
            !string.Equals(currentEmail, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var identity = new ClaimsIdentity(
                AppClaimsFactory.CreateClaims(user, sessionToken),
                AuthConstants.AuthenticationScheme);

            context.ReplacePrincipal(new ClaimsPrincipal(identity));
            context.ShouldRenew = true;
        }
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(AuthConstants.AuthenticationScheme);
    }
}
