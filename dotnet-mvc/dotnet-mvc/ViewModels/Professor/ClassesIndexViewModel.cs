namespace dotnet_mvc.ViewModels.Professor;

public class ClassesIndexViewModel
{
    public IReadOnlyList<ClassroomListItemViewModel> Classes { get; set; } = [];
    public int TotalStudents { get; set; }
    public int TotalChallenges { get; set; }
    public int TotalSubmittedChallenges { get; set; }
    public int TotalReviewedChallenges { get; set; }
    public string OverallCompletionRateLabel { get; set; } = "0%";
}
