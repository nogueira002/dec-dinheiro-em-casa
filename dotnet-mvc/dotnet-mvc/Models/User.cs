namespace dotnet_mvc.Models;

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Gender { get; set; }
    public DateTime? BirthDate { get; set; }

    public string Email { get; set; } = string.Empty;
    public string? ImgUrl { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";
    public string UserStatus { get; set; } = "Active";

    public DateTime? LastLoginAtUtc { get; set; }

    public ICollection<Classroom> ManagedClassrooms { get; set; } = [];
    public ICollection<ClassroomMembership> ClassroomMemberships { get; set; } = [];
    public ICollection<ChallengeEnrollment> ChallengeEnrollments { get; set; } = [];
    public ICollection<ChallengeSubmission> ChallengeSubmissions { get; set; } = [];
    public ICollection<Challenge> CreatedChallenges { get; set; } = [];
    public ICollection<Scenario> Scenarios { get; set; } = [];
    public ICollection<SimulationHistory> SimulationHistories { get; set; } = [];
}
