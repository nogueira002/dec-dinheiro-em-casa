using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace dotnet_mvc.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "server=localhost;port=3307;database=dec_db;user=root;password=root";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new AppDbContext(optionsBuilder.Options);
    }
}
