namespace dotnet_mvc.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int StudentCount { get; set; }
    public int ProfessorCount { get; set; }
    public int AdminCount { get; set; }
    public int ActiveCount { get; set; }
    public int TotalClassrooms { get; set; }
    public int TotalChallenges { get; set; }
    public int TotalSubmissions { get; set; }
    public int PendingSubmissions { get; set; }
}
