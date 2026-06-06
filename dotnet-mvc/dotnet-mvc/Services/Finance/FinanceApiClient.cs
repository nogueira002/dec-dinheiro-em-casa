using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace dotnet_mvc.Services.Finance;

// Resultado da chamada de simulação: ou traz o resultado, ou uma mensagem de erro
// amigável para apresentar ao utilizador. Nunca os dois em simultâneo.
public sealed record SimulationOutcome(SimulationResult? Result, string? ErrorMessage)
{
    public bool Success => Result is not null;

    public static SimulationOutcome Ok(SimulationResult result) => new(result, null);
    public static SimulationOutcome Fail(string message) => new(null, message);
}

public interface IFinanceApiClient
{
    // Pede uma simulação financeira à API Django (que invoca o motor Python).
    Task<SimulationOutcome> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default);
}

public sealed class FinanceApiClient : IFinanceApiClient
{
    private const string GenericError =
        "Não foi possível obter a simulação. Tenta novamente dentro de momentos.";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<FinanceApiClient> _logger;

    public FinanceApiClient(HttpClient httpClient, ILogger<FinanceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SimulationOutcome> SimulateAsync(SimulationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Serializa para um buffer e envia com Content-Length explícito.
            // Importante: evita Transfer-Encoding: chunked, que o servidor de
            // desenvolvimento do Django (wsgiref/runserver) não consegue ler —
            // o corpo chegaria vazio e a simulação corria com valores a zero.
            var json = JsonSerializer.Serialize(request, SerializerOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // BaseAddress termina em "/api/", logo o caminho relativo é "simulate/".
            using var response = await _httpClient.PostAsync("simulate/", content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SimulationResult>(SerializerOptions, cancellationToken);
                return result is not null
                    ? SimulationOutcome.Ok(result)
                    : SimulationOutcome.Fail(GenericError);
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "API de cálculo respondeu {StatusCode} ao simular — corpo: {Body}",
                (int)response.StatusCode, body);

            return SimulationOutcome.Fail(ExtractErrorMessage(body));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogError(ex, "Falha ao comunicar com a API de cálculo financeiro.");
            return SimulationOutcome.Fail(GenericError);
        }
    }

    // Tenta extrair a mensagem de erro devolvida pela API Django.
    // Formatos possíveis: {"erro": "..."} ou os erros de validação do serializer.
    private string ExtractErrorMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return GenericError;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("erro", out var erro) &&
                erro.ValueKind == JsonValueKind.String)
            {
                return erro.GetString() ?? GenericError;
            }

            // Erros de validação do DRF: { "campo": ["mensagem", ...], ... }
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array &&
                        property.Value.GetArrayLength() > 0)
                    {
                        var first = property.Value[0];
                        if (first.ValueKind == JsonValueKind.String)
                        {
                            return first.GetString() ?? GenericError;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Corpo não-JSON (ex.: página de erro HTML do Django) — usa a mensagem genérica.
        }

        return GenericError;
    }
}
