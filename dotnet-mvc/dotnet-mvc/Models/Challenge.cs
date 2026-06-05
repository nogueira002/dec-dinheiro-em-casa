namespace dotnet_mvc.Models;

public class Challenge
{
    public int Id { get; set; }

    public int? ClassroomId { get; set; }
    public Classroom? Classroom { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccessCode { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public bool NotifyStudents { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<ChallengeEnrollment> Enrollments { get; set; } = [];
    public ICollection<ChallengeSubmission> Submissions { get; set; } = [];
}
