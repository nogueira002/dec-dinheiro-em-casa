namespace dotnet_mvc.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";
    public string UserStatus { get; set; } = "Active";

    public string? SessionTokenHash { get; set; }
    public DateTime? SessionExpiresAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
