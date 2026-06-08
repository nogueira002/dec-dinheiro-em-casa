namespace dotnet_mvc.ViewModels.Admin;

public class ServiceStatusViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public long? ResponseTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MonitoringViewModel
{
    public ServiceStatusViewModel Portal { get; set; } = new();
    public ServiceStatusViewModel DjangoApi { get; set; } = new();
    public ServiceStatusViewModel Database { get; set; } = new();
    public DateTime CheckedAtUtc { get; set; }
}
