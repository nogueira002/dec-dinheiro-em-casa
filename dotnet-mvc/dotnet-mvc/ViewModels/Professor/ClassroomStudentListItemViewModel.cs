namespace dotnet_mvc.ViewModels.Professor;

public class ClassroomStudentListItemViewModel
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
    public string AssociationLabel { get; set; } = string.Empty;
    public string? ChallengeSummary { get; set; }
}
