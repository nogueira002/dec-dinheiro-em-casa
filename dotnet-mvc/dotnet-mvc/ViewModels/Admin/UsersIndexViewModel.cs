namespace dotnet_mvc.ViewModels.Admin;

public class UsersIndexViewModel
{
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = [];
    public string? Search { get; set; }
    public string? RoleFilter { get; set; }
    public string? StatusFilter { get; set; }
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int StudentCount { get; set; }
    public int ProfessorCount { get; set; }
}
