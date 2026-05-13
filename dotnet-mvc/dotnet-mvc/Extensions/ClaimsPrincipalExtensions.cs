using System.Security.Claims;
using dotnet_mvc.Security;

namespace dotnet_mvc.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : null;
    }

    public static string GetDisplayName(this ClaimsPrincipal principal)
    {
        return principal.Identity?.Name ?? "Utilizador";
    }

    public static string GetRoleLabel(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(AuthConstants.RoleLabelClaimType)
            ?? UserRoles.ToDisplayLabel(principal.FindFirstValue(ClaimTypes.Role));
    }
}
