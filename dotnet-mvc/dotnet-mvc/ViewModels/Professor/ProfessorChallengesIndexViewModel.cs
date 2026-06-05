namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorChallengesIndexViewModel
{
    public IReadOnlyList<ProfessorChallengeListItemViewModel> Challenges { get; set; } = [];
    public int DraftCount { get; set; }
    public int PublishedCount { get; set; }
}
