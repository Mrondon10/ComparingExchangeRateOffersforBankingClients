using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using ExchangeRateSystem.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExchangeRateSystem.Tests.Services;

public class ExchangeRateServiceTests
{
    private readonly Mock<ILogger<ExchangeRateService>> _loggerMock;
    private readonly List<Mock<IExchangeRateProvider>> _providerMocks;
    private readonly ExchangeRateService _service;

    public ExchangeRateServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExchangeRateService>>();
        _providerMocks = new List<Mock<IExchangeRateProvider>>();

        for (int i = 0; i < 3; i++)
        {
            var providerMock = new Mock<IExchangeRateProvider>();
            providerMock.Setup(p => p.ProviderName).Returns($"Provider{i + 1}");
            _providerMocks.Add(providerMock);
        }

        _service = new ExchangeRateService(
           _providerMocks.Select(m => m.Object),
               _loggerMock.Object);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenAllProvidersSucceed_ReturnsHighestRate()
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        _providerMocks[0].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
 .ReturnsAsync(ApiResult<decimal>.Success(85.5m, "Provider1"));

        _providerMocks[1].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
       .ReturnsAsync(ApiResult<decimal>.Success(86.2m, "Provider2"));

        _providerMocks[2].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(ApiResult<decimal>.Success(84.8m, "Provider3"));

        var result = await _service.GetBestExchangeRateAsync(request);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be("200");
        result.Message.Should().Be("Éxito");
        result.Data.Should().NotBeNull();
        result.Data!.ConvertedAmount.Should().Be(86.2m);
        result.Data.Provider.Should().Be("Provider2");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenSomeProvidersFail_ReturnsHighestFromSuccessful()
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = "USD",
            TargetCurrency = "GBP",
            Amount = 100
        };

        _providerMocks[0].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(ApiResult<decimal>.Failure("API Error", "Provider1"));

        _providerMocks[1].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(ApiResult<decimal>.Success(75.5m, "Provider2"));

        _providerMocks[2].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
  .ReturnsAsync(ApiResult<decimal>.Success(76.0m, "Provider3"));

        var result = await _service.GetBestExchangeRateAsync(request);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be("200");
        result.Data.Should().NotBeNull();
        result.Data!.ConvertedAmount.Should().Be(76.0m);
        result.Data.Provider.Should().Be("Provider3");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WhenAllProvidersFail_ReturnsError()
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = "USD",
            TargetCurrency = "JPY",
            Amount = 100
        };

        foreach (var mock in _providerMocks)
        {
            mock.Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<decimal>.Failure("Service unavailable", mock.Object.ProviderName));
        }

        var result = await _service.GetBestExchangeRateAsync(request);

        result.Should().NotBeNull();
        result.StatusCode.Should().Be("500");
        result.Message.Should().Contain("proveedores");
        result.Data.Should().BeNull();
    }

    [Theory]
    [InlineData("", "EUR", 100, "moneda de origen")]
    [InlineData("USD", "", 100, "moneda de destino")]
    [InlineData("USD", "EUR", 0, "debe ser mayor a cero")]
    [InlineData("USD", "EUR", -50, "debe ser mayor a cero")]
    [InlineData("US", "EUR", 100, "código ISO de 3 letras")]
    [InlineData("USD", "EU", 100, "código ISO de 3 letras")]
    public async Task GetBestExchangeRateAsync_WithInvalidRequest_ThrowsArgumentException(
         string from, string to, decimal amount, string expectedMessage)
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = from,
            TargetCurrency = to,
            Amount = amount
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
   () => _service.GetBestExchangeRateAsync(request));
        exception.Message.Should().Contain(expectedMessage);
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_WithNullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.GetBestExchangeRateAsync(null!));
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_UppercasesCurrencyCodes()
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = "usd",
            TargetCurrency = "eur",
            Amount = 100
        };

        _providerMocks[0].Setup(p => p.GetExchangeRateAsync("usd", "eur", 100, It.IsAny<CancellationToken>()))
   .ReturnsAsync(ApiResult<decimal>.Success(85.0m, "Provider1"));

        var result = await _service.GetBestExchangeRateAsync(request);

        result.Data.Should().NotBeNull();
        result.Data!.SourceCurrency.Should().Be("USD");
        result.Data.TargetCurrency.Should().Be("EUR");
    }

    [Fact]
    public async Task GetBestExchangeRateAsync_CalculatesExchangeRateCorrectly()
    {
        var request = new ExchangeRateRequest
        {
            SourceCurrency = "USD",
            TargetCurrency = "EUR",
            Amount = 100
        };

        _providerMocks[0].Setup(p => p.GetExchangeRateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApiResult<decimal>.Success(85.0m, "Provider1"));

        var result = await _service.GetBestExchangeRateAsync(request);

        result.Data.Should().NotBeNull();
        result.Data!.ExchangeRate.Should().Be(0.85m);
        result.Data.Amount.Should().Be(100);
        result.Data.ConvertedAmount.Should().Be(85.0m);
    }
}
