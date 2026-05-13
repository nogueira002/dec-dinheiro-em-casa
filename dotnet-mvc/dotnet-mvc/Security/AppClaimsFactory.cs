using System.Security.Claims;
using dotnet_mvc.Models;

namespace dotnet_mvc.Security;

public static class AppClaimsFactory
{
    public static IEnumerable<Claim> CreateClaims(User user, string sessionToken)
    {
        return
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(AuthConstants.SessionTokenClaimType, sessionToken),
            new Claim(AuthConstants.RoleLabelClaimType, UserRoles.ToDisplayLabel(user.Role))
        ];
    }
}
