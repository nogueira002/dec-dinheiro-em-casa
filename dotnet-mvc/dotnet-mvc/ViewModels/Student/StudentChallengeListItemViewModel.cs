namespace dotnet_mvc.ViewModels.Student;

public class StudentChallengeListItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Professor { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public string StatusKey { get; set; } = "pending";
    public string StatusLabel { get; set; } = "Pendente";
    public string? Grade { get; set; }
}
