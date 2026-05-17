namespace dotnet_mvc.Models;

public class Classroom
{
    public int Id { get; set; }

    public int OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccessCode { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ClassroomMembership> Memberships { get; set; } = [];
    public ICollection<Challenge> Challenges { get; set; } = [];
}
