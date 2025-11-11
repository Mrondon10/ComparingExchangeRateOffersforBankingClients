using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRateSystem.Infrastructure.Providers;

public class MockJsonApi2ExchangeRateProvider : IExchangeRateProvider
{
    private readonly ILogger<MockJsonApi2ExchangeRateProvider> _logger;
    private readonly Random _random = new();

    public string ProviderName => "MockJsonAPI2";

    public MockJsonApi2ExchangeRateProvider(ILogger<MockJsonApi2ExchangeRateProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApiResult<decimal>> GetExchangeRateAsync(
 string sourceCurrency,
  string targetCurrency,
decimal amount,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Mock {Provider} API simulating conversion {From} -> {To}",
           ProviderName, sourceCurrency, targetCurrency);

            await Task.Delay(120, cancellationToken);

            var exchangeRate = GenerateMockExchangeRate(sourceCurrency, targetCurrency);
            var convertedAmount = amount * exchangeRate;

            _logger.LogDebug("{Provider} returned converted amount: {Amount}",
                ProviderName, convertedAmount);

            return ApiResult<decimal>.Success(convertedAmount, ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Provider} unexpected error", ProviderName);
            return ApiResult<decimal>.Failure($"Error inesperado: {ex.Message}", ProviderName);
        }
    }

    private decimal GenerateMockExchangeRate(string from, string to)
    {
        var baseRate = (from, to) switch
        {
            ("USD", "EUR") => 0.84m,
            ("EUR", "USD") => 1.19m,
            ("USD", "GBP") => 0.72m,
            ("GBP", "USD") => 1.39m,
            ("USD", "JPY") => 109.0m,
            ("JPY", "USD") => 0.0092m,
            _ => 1.0m
        };

        var variation = (decimal)(_random.NextDouble() * 0.04 - 0.02);
        return baseRate * (1 + variation);
    }
}
