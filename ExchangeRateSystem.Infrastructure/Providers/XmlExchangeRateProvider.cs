using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using ExchangeRateSystem.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace ExchangeRateSystem.Infrastructure.Providers;

public class XmlExchangeRateProvider : IExchangeRateProvider
{
    private readonly HttpClient _httpClient;
    private readonly XmlApiSettings _settings;
    private readonly ILogger<XmlExchangeRateProvider> _logger;

    public string ProviderName => "XmlAPI";

    public XmlExchangeRateProvider(
        HttpClient httpClient,
        IOptions<ExchangeRateApiSettings> settings,
        ILogger<XmlExchangeRateProvider> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value?.XmlApi ?? throw new ArgumentNullException(nameof(settings));
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
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
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

            var xmlRequest = new XElement("XML",
                  new XElement("From", sourceCurrency.ToUpper()),
                  new XElement("To", targetCurrency.ToUpper()),
               new XElement("Amount", amount)
                  );

            var content = new StringContent(xmlRequest.ToString(), System.Text.Encoding.UTF8, "application/xml");
            var response = await _httpClient.PostAsync("convert", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("{Provider} API returned status {Status}: {Error}",
                      ProviderName, response.StatusCode, errorContent);
                return ApiResult<decimal>.Failure($"La API retornó {response.StatusCode}", ProviderName);
            }

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var xmlResponse = XDocument.Parse(responseContent);

            var resultElement = xmlResponse.Root?.Element("Result");
            if (resultElement == null || !decimal.TryParse(resultElement.Value, out var result))
            {
                _logger.LogWarning("{Provider} API returned invalid XML response", ProviderName);
                return ApiResult<decimal>.Failure("Respuesta XML inválida", ProviderName);
            }

            _logger.LogDebug("{Provider} returned converted amount: {Amount}", ProviderName, result);
            return ApiResult<decimal>.Success(result, ProviderName);
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
}
