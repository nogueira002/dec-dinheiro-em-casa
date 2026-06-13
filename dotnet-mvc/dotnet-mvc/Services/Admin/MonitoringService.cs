using System.Diagnostics;
using dotnet_mvc.Models.DAL;
using dotnet_mvc.ViewModels.Admin;
using MySqlConnector;

namespace dotnet_mvc.Services.Admin;

public interface IMonitoringService
{
    Task<MonitoringViewModel> GetStatusAsync(CancellationToken ct = default);
}

public sealed class MonitoringService : IMonitoringService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration     _configuration;

    public MonitoringService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration     = configuration;
    }

    public async Task<MonitoringViewModel> GetStatusAsync(CancellationToken ct = default)
    {
        var dbTask  = CheckDatabaseAsync(ct);
        var apiTask = CheckDjangoApiAsync(ct);

        await Task.WhenAll(dbTask, apiTask);

        return new MonitoringViewModel
        {
            Portal = new ServiceStatusViewModel
            {
                Name           = "Portal (.NET)",
                Description    = "Aplicação ASP.NET Core 8 MVC",
                IsOnline       = true,
                ResponseTimeMs = null
            },
            Database   = await dbTask,
            DjangoApi  = await apiTask,
            CheckedAtUtc = DateTime.UtcNow
        };
    }

    private static async Task<ServiceStatusViewModel> CheckDatabaseAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new MySqlConnection(DriverDB.ConnectionString);
            await conn.OpenAsync(ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await cmd.ExecuteScalarAsync(ct);
            sw.Stop();
            return new ServiceStatusViewModel
            {
                Name           = "MySQL",
                Description    = "Base de dados principal",
                IsOnline       = true,
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new ServiceStatusViewModel
            {
                Name           = "MySQL",
                Description    = "Base de dados principal",
                IsOnline       = false,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ErrorMessage   = ex.Message
            };
        }
    }

    private async Task<ServiceStatusViewModel> CheckDjangoApiAsync(CancellationToken ct)
    {
        var baseUrl = _configuration["FinanceApi:BaseUrl"] ?? "http://localhost:8000/api/";
        var sw      = Stopwatch.StartNew();
        try
        {
            var client   = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync(baseUrl, ct);
            sw.Stop();
            return new ServiceStatusViewModel
            {
                Name           = "API Django",
                Description    = "Motor de simulações financeiras",
                IsOnline       = response.IsSuccessStatusCode || (int)response.StatusCode < 500,
                ResponseTimeMs = sw.ElapsedMilliseconds
            };
        }
        catch
        {
            sw.Stop();
            return new ServiceStatusViewModel
            {
                Name           = "API Django",
                Description    = "Motor de simulações financeiras",
                IsOnline       = false,
                ResponseTimeMs = sw.ElapsedMilliseconds,
                ErrorMessage   = "Serviço inacessível"
            };
        }
    }
}
