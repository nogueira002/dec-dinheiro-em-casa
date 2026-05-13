using Microsoft.AspNetCore.Authentication.Cookies;

namespace dotnet_mvc.Security;

public static class AuthConstants
{
    public const string AuthenticationScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    public const string SessionTokenClaimType = "dec:session_token";
    public const string RoleLabelClaimType = "dec:role_label";

    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);
    public static readonly TimeSpan PersistentSessionLifetime = TimeSpan.FromDays(30);
}
