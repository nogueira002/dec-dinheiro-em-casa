namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorStudentListItemViewModel
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public DateTime JoinedAtUtc { get; set; }
    public string AssociationLabel { get; set; } = string.Empty;
    public int AvailableChallengesCount { get; set; }
    public int ActiveChallengesCount { get; set; }
    public string ChallengeSummary { get; set; } = string.Empty;
}
