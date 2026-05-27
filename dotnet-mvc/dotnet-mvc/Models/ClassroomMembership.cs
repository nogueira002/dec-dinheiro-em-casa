namespace dotnet_mvc.Models;

public class ClassroomMembership
{
    public int Id { get; set; }

    public int ClassroomId { get; set; }
    public Classroom Classroom { get; set; } = null!;

    public int StudentId { get; set; }
    public User Student { get; set; } = null!;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
