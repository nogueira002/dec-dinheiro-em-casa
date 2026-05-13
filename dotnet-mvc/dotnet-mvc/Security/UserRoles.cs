namespace dotnet_mvc.Security;

public static class UserRoles
{
    public const string Student = "Student";
    public const string Professor = "Professor";
    public const string Admin = "Admin";

    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Student,
        Professor,
        Admin
    };

    public static bool IsSupported(string? role)
    {
        return !string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role);
    }

    public static string Normalize(string? role)
    {
        return role?.Trim() switch
        {
            Student => Student,
            Professor => Professor,
            Admin => Admin,
            _ => Student
        };
    }

    public static string ToDisplayLabel(string? role)
    {
        return Normalize(role) switch
        {
            Professor => "Professor",
            Admin => "Administrador",
            _ => "Aluno"
        };
    }

    public static (string Controller, string Action) GetDefaultRoute(string? role)
    {
        return Normalize(role) switch
        {
            Professor => ("Professor", "Dashboard"),
            Admin => ("Admin", "Dashboard"),
            _ => ("Student", "Dashboard")
        };
    }
}
