using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using ExchangeRateSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace ExchangeRateSystem.Infrastructure.Providers;

public class JsonApi2ExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly JsonApi2Settings _settings;
    private readonly ILogger<JsonApi2ExchangeRateProvider> _logger;

    public string ProviderName => "JsonAPI2";

    public JsonApi2ExchangeRateProvider(
 HttpClient httpClient,
  IOptions<ExchangeRateApiSettings> settings,
  ILogger<JsonApi2ExchangeRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value?.JsonApi2 ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        }

        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Api-Key", _settings.ApiKey);
        }
    }

    public async Task<ApiResult<decimal>> GetExchangeRateAsync(
           string sourceCurrency,
    string targetCurrency,
     decimal amount,
           CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Calling {Provider} API for {From} -> {To}", ProviderName, sourceCurrency, targetCurrency);

            var requestBody = new
            {
                exchange = new
                {
                    sourceCurrency = sourceCurrency.ToUpper(),
                    targetCurrency = targetCurrency.ToUpper(),
                    quantity = amount
                }
            };

            var response = await _httpClient.PostAsJsonAsync("exchange", requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("{Provider} API returned status {Status}: {Error}",
                       ProviderName, response.StatusCode, errorContent);
                return ApiResult<decimal>.Failure($"La API retornó {response.StatusCode}", ProviderName);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<JsonApi2Response>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Data?.Total == null)
            {
                _logger.LogWarning("{Provider} API returned invalid response", ProviderName);
                return ApiResult<decimal>.Failure("Respuesta inválida de la API", ProviderName);
            }

            var convertedAmount = result.Data.Total.Value;
            _logger.LogDebug("{Provider} returned converted amount: {Amount}", ProviderName, convertedAmount);

            return ApiResult<decimal>.Success(convertedAmount, ProviderName);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "{Provider} HTTP request failed", ProviderName);
            return ApiResult<decimal>.Failure($"Falló la solicitud HTTP: {ex.Message}", ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Provider} unexpected error", ProviderName);
            return ApiResult<decimal>.Failure($"Error inesperado: {ex.Message}", ProviderName);
        }
    }

    private class JsonApi2Response
    {
        public string? StatusCode { get; set; }
        public string? Message { get; set; }
        public JsonApi2Data? Data { get; set; }
    }

    private class JsonApi2Data
    {
        public decimal? Total { get; set; }
    }
}
