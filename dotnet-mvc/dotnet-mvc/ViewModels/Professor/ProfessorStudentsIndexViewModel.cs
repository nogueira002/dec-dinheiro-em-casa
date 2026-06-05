namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorStudentsIndexViewModel
{
    public int? SelectedClassId { get; set; }
    public IReadOnlyList<ClassroomOptionViewModel> AvailableClasses { get; set; } = [];
    public IReadOnlyList<ProfessorStudentListItemViewModel> Students { get; set; } = [];
    public int TotalStudents { get; set; }
    public int TotalAvailableChallenges { get; set; }
    public int TotalSubmittedChallenges { get; set; }
    public int TotalReviewedChallenges { get; set; }
    public string OverallCompletionRateLabel { get; set; } = "0%";
}
