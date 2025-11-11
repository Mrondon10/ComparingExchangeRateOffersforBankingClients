using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using Microsoft.Extensions.Logging;

namespace ExchangeRateSystem.Infrastructure.Providers;

public class MockJsonExchangeRateProvider : IExchangeRateProvider
{
    private readonly ILogger<MockJsonExchangeRateProvider> _logger;
    private readonly Random _random = new();

    public string ProviderName => "MockJsonAPI";

    public MockJsonExchangeRateProvider(ILogger<MockJsonExchangeRateProvider> logger)
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

            await Task.Delay(100, cancellationToken);

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
            ("USD", "EUR") => 0.85m,
            ("EUR", "USD") => 1.18m,
            ("USD", "GBP") => 0.73m,
            ("GBP", "USD") => 1.37m,
            ("USD", "JPY") => 110.0m,
            ("JPY", "USD") => 0.009m,
            _ => 1.0m
        };

        var variation = (decimal)(_random.NextDouble() * 0.04 - 0.02);
        return baseRate * (1 + variation);
    }
}
