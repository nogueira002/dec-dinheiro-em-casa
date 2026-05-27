using System.Security.Cryptography;
using dotnet_mvc.Data;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Services.Academic;

public interface IAccessCodeService
{
    string GeneratePreview();
    Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default);
}

public sealed class AccessCodeService : IAccessCodeService
{
    private static readonly char[] Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();
    private readonly AppDbContext _dbContext;

    public AccessCodeService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string GeneratePreview()
    {
        return BuildCode();
    }

    public async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = BuildCode();
            var exists = await _dbContext.Classrooms.AnyAsync(x => x.AccessCode == code, cancellationToken)
                || await _dbContext.Challenges.AnyAsync(x => x.AccessCode == code, cancellationToken);

            if (!exists)
            {
                return code;
            }
        }

        throw new InvalidOperationException("Não foi possível gerar um código de adesão único.");
    }

    private static string BuildCode()
    {
        Span<byte> buffer = stackalloc byte[5];
        RandomNumberGenerator.Fill(buffer);

        Span<char> segment = stackalloc char[5];
        for (var i = 0; i < buffer.Length; i++)
        {
            segment[i] = Alphabet[buffer[i] % Alphabet.Length];
        }

        return $"DEC-{new string(segment)}";
    }
}
