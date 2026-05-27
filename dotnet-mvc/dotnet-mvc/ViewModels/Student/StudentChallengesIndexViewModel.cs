namespace dotnet_mvc.ViewModels.Student;

public class StudentChallengesIndexViewModel
{
    public IReadOnlyList<StudentChallengeListItemViewModel> Challenges { get; set; } = [];
}
