using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace dotnet_mvc.Security;

public interface ISessionTokenService
{
    string GenerateToken();
    string HashToken(string token);
    bool Matches(string token, string tokenHash);
}

public sealed class SessionTokenService : ISessionTokenService
{
    public string GenerateToken()
    {
        return WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public bool Matches(string token, string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(tokenHash))
        {
            return false;
        }

        var computed = HashToken(token);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(tokenHash));
    }
}
