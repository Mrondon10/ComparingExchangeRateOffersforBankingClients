using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRateSystem.Core.Services;

public class ExchangeRateService : IExchangeRateService
{
    private readonly IEnumerable<IExchangeRateProvider> _providers;
    private readonly ILogger<ExchangeRateService> _logger;

    public ExchangeRateService(
        IEnumerable<IExchangeRateProvider> providers,
        ILogger<ExchangeRateService> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ExchangeRateResponse> GetBestExchangeRateAsync(
 ExchangeRateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ValidateRequest(request);

        _logger.LogInformation(
            "Processing exchange rate request: {From} -> {To}, Amount: {Amount}",
         request.SourceCurrency,
   request.TargetCurrency,
            request.Amount);

        var tasks = _providers.Select(provider =>
   GetRateWithTimeoutAsync(provider, request, cancellationToken));

        var results = await Task.WhenAll(tasks);

        var successfulResults = results
   .Where(r => r.IsSuccess)
.ToList();

        if (!successfulResults.Any())
        {
            _logger.LogWarning("No providers returned successful results");
            return new ExchangeRateResponse
            {
                StatusCode = "500",
                Message = "Todos los proveedores de tasas de cambio fallaron o no están disponibles",
                Data = null
            };
        }

        var bestResult = successfulResults
            .OrderByDescending(r => r.Data)
            .First();

        _logger.LogInformation(
            "Best rate found from {Provider}: {Rate}",
                 bestResult.Source,
              bestResult.Data);

        return new ExchangeRateResponse
        {
            StatusCode = "200",
            Message = "Éxito",
            Data = new ExchangeRateData
            {
                SourceCurrency = request.SourceCurrency.ToUpper(),
                TargetCurrency = request.TargetCurrency.ToUpper(),
                Amount = request.Amount,
                ConvertedAmount = bestResult.Data,
                ExchangeRate = bestResult.Data / request.Amount,
                Provider = bestResult.Source,
                Timestamp = DateTime.UtcNow
            }
        };
    }

    private async Task<ApiResult<decimal>> GetRateWithTimeoutAsync(
      IExchangeRateProvider provider,
        ExchangeRateRequest request,
     CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            _logger.LogDebug("Querying provider: {Provider}", provider.ProviderName);

            var result = await provider.GetExchangeRateAsync(
          request.SourceCurrency,
                request.TargetCurrency,
       request.Amount,
         cts.Token);

            if (result.IsSuccess)
            {
                _logger.LogDebug(
                  "Provider {Provider} returned rate: {Rate}",
                    provider.ProviderName,
        result.Data);
            }
            else
            {
                _logger.LogWarning(
             "Provider {Provider} failed: {Error}",
      provider.ProviderName,
            result.ErrorMessage);
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Provider {Provider} timed out", provider.ProviderName);
            return ApiResult<decimal>.Failure("Tiempo de espera agotado", provider.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provider {Provider} threw exception", provider.ProviderName);
            return ApiResult<decimal>.Failure(ex.Message, provider.ProviderName);
        }
    }

    private void ValidateRequest(ExchangeRateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceCurrency))
            throw new ArgumentException("La moneda de origen es requerida", nameof(request.SourceCurrency));

        if (string.IsNullOrWhiteSpace(request.TargetCurrency))
            throw new ArgumentException("La moneda de destino es requerida", nameof(request.TargetCurrency));

        if (request.Amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero", nameof(request.Amount));

        if (request.SourceCurrency.Length != 3)
            throw new ArgumentException("La moneda de origen debe ser un código ISO de 3 letras", nameof(request.SourceCurrency));

        if (request.TargetCurrency.Length != 3)
            throw new ArgumentException("La moneda de destino debe ser un código ISO de 3 letras", nameof(request.TargetCurrency));
    }
}
