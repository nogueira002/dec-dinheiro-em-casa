using Microsoft.AspNetCore.Authentication.Cookies;

namespace dotnet_mvc.Security;

// Constantes partilhadas pelo sistema de autenticação
public static class AuthConstants
{
    // Nome do esquema de autenticação — usa cookies (padrão do ASP.NET Core)
    public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;

    // Claim personalizado para guardar o nome legível do role na cookie (ex: "Aluno", "Professor")
    public const string RoleLabelClaimType = "dec:role_label";

    // Sessão normal: expira ao fim de 8 horas
    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    // Sessão "lembrar-me": expira ao fim de 30 dias
    public static readonly TimeSpan PersistentSessionLifetime = TimeSpan.FromDays(30);
}
