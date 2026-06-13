using System.Security.Cryptography;
using dotnet_mvc.Models.DAL;
using MySqlConnector;

namespace dotnet_mvc.Services.Academic;

public interface IAccessCodeService
{
    string GeneratePreview();
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default);
}

public sealed class AccessCodeService : IAccessCodeService
{
    private static readonly char[] Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    public string GeneratePreview() => BuildCode();

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = BuildCode();
            var exists = await CodeExistsAsync(code, cancellationToken);
            if (!exists) return code;
        }

        throw new InvalidOperationException("Não foi possível gerar um código de adesão único.");
    }

    private static async Task<bool> CodeExistsAsync(string code, CancellationToken ct)
    {
        await using var conn = new MySqlConnection(DriverDB.ConnectionString);
        await conn.OpenAsync(ct);
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*) FROM (
                SELECT AccessCode FROM Classrooms WHERE AccessCode = @c
                UNION ALL
                SELECT AccessCode FROM Challenges  WHERE AccessCode = @c
            ) t
            """;
        cmd.Parameters.AddWithValue("@c", code);
        var count = Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
        return count > 0;
    }

    private static string BuildCode()
    {
        Span<byte> buffer  = stackalloc byte[5];
        RandomNumberGenerator.Fill(buffer);
        Span<char> segment = stackalloc char[5];
        for (var i = 0; i < buffer.Length; i++)
            segment[i] = Alphabet[buffer[i] % Alphabet.Length];
        return $"DEC-{new string(segment)}";
    }
}
