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
    }
}
