using ExchangeRateSystem.Core.Models;

namespace ExchangeRateSystem.Core.Interfaces;

public interface IExchangeRateService
{
    Task<ExchangeRateResponse> GetBestExchangeRateAsync(ExchangeRateRequest request, CancellationToken cancellationToken = default);
}
