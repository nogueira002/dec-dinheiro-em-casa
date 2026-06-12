using System.Security.Claims;
using dotnet_mvc.Models;

namespace dotnet_mvc.Security;

// Cria a lista de dados do utilizador que ficam guardados na cookie de sessão.
// Estes dados ficam disponíveis em qualquer controller via User.FindFirstValue(...)
public static class AppClaimsFactory
{
    public static List<Claim> CriarClaims(User utilizador)
    {
        return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, utilizador.Id.ToString()),
            new Claim(ClaimTypes.Name,           utilizador.Name),
            new Claim(ClaimTypes.Email,          utilizador.Email),
            new Claim(ClaimTypes.Role,           utilizador.Role),
            new Claim(AuthConstants.RoleLabelClaimType, UserRoles.ToDisplayLabel(utilizador.Role))
        };
    }
}
