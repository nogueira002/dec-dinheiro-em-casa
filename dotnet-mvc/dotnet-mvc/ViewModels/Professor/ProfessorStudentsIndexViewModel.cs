namespace dotnet_mvc.ViewModels.Professor;

public class ProfessorStudentsIndexViewModel
{
    public int? SelectedClassId { get; set; }
    public IReadOnlyList<ClassroomOptionViewModel> AvailableClasses { get; set; } = [];
    public IReadOnlyList<ProfessorStudentListItemViewModel> Students { get; set; } = [];
}
