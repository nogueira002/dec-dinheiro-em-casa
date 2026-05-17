namespace dotnet_mvc.Models;

public class ChallengeEnrollment
{
    public int Id { get; set; }

    public int ChallengeId { get; set; }
    public Challenge Challenge { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
