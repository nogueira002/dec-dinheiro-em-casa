using dotnet_mvc.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_mvc.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Classroom> Classrooms { get; set; }
    public DbSet<ClassroomMembership> ClassroomMemberships { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<ChallengeEnrollment> ChallengeEnrollments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Name).HasMaxLength(120);
            entity.Property(u => u.Email).HasMaxLength(256);
            entity.Property(u => u.PasswordHash).HasMaxLength(512);
            entity.Property(u => u.Role).HasMaxLength(32);
            entity.Property(u => u.UserStatus).HasMaxLength(32);
            entity.Property(u => u.SessionTokenHash).HasMaxLength(128);
        });

        modelBuilder.Entity<Classroom>(entity =>
        {
            entity.HasIndex(x => x.AccessCode).IsUnique();

            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(500);
            entity.Property(x => x.AccessCode).HasMaxLength(16);

            entity.HasOne(x => x.Owner)
                .WithMany(x => x.ManagedClassrooms)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ClassroomMembership>(entity =>
        {
            entity.HasIndex(x => new { x.ClassroomId, x.StudentId }).IsUnique();

            entity.HasOne(x => x.Classroom)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Student)
                .WithMany(x => x.ClassroomMemberships)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Challenge>(entity =>
        {
            entity.HasIndex(x => x.AccessCode).IsUnique();

            entity.Property(x => x.Title).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.AccessCode).HasMaxLength(16);

            entity.HasOne(x => x.Classroom)
                .WithMany(x => x.Challenges)
                .HasForeignKey(x => x.ClassroomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedChallenges)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChallengeEnrollment>(entity =>
        {
            entity.HasIndex(x => new { x.ChallengeId, x.StudentId }).IsUnique();

            entity.HasOne(x => x.Challenge)
                .WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.ChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Student)
                .WithMany(x => x.ChallengeEnrollments)
                .HasForeignKey(x => x.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
