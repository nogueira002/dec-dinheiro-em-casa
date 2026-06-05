namespace dotnet_mvc.Models;

public class ChallengeSubmission
{
    public int Id { get; set; }

    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public string ResponseText { get; set; } = string.Empty;
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public decimal? Grade { get; set; }
    public string? Feedback { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
}
