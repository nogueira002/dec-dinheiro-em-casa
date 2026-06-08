namespace dotnet_mvc.ViewModels.Admin;

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
}
