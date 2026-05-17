namespace dotnet_mvc.ViewModels.Professor;

public class ClassesIndexViewModel
{
    public IReadOnlyList<ClassroomListItemViewModel> Classes { get; set; } = [];
    public int TotalStudents { get; set; }
    public int TotalChallenges { get; set; }
}
