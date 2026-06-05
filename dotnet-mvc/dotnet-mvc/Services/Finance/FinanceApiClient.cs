using System.Net.Http.Json;
using System.Text.Json;

namespace dotnet_mvc.Services.Finance;

public interface IFinanceApiClient
{
    // Pede uma simulação financeira à API Django (que invoca o motor Python).
    // Devolve null se a API responder com erro ou estiver indisponível.
    Task<SimulationResult?> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default);
}

public sealed class FinanceApiClient : IFinanceApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<FinanceApiClient> _logger;

    public FinanceApiClient(HttpClient httpClient, ILogger<FinanceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SimulationResult?> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // BaseAddress termina em "/api/", logo o caminho relativo é "simulate/".
            using var response = await _httpClient.PostAsJsonAsync("simulate/", request, SerializerOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "API de cálculo respondeu {StatusCode} ao simular — corpo: {Body}",
                    (int)response.StatusCode, body);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SimulationResult>(SerializerOptions, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha ao comunicar com a API de cálculo financeiro.");
            return null;
        }
    }
}
