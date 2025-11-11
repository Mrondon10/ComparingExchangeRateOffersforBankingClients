using ExchangeRateSystem.Core.Models;

namespace ExchangeRateSystem.Core.Interfaces;

public interface IExchangeRateProvider
{
    string ProviderName { get; }
    Task<ApiResult<decimal>> GetExchangeRateAsync(string sourceCurrency, string targetCurrency, decimal amount, CancellationToken cancellationToken = default);
}
