namespace dotnet_mvc.ViewModels.Professor;

public class ClassroomChallengeListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public bool IsExpired { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public int AudienceCount { get; set; }
    public int DirectParticipantCount { get; set; }
}
