using ExchangeRateSystem.Core.Models;
using ExchangeRateSystem.Infrastructure.Configuration;
using ExchangeRateSystem.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace ExchangeRateSystem.Tests.Providers;

public class JsonExchangeRateProviderTests
{
    private readonly Mock<ILogger<JsonExchangeRateProvider>> _loggerMock;
    private readonly Mock<IOptions<ExchangeRateApiSettings>> _settingsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public JsonExchangeRateProviderTests()
    {
        _loggerMock = new Mock<ILogger<JsonExchangeRateProvider>>();
        _settingsMock = new Mock<IOptions<ExchangeRateApiSettings>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test.com/")
        };

        var settings = new ExchangeRateApiSettings
        {
            JsonApi = new JsonApiSettings
            {
                BaseUrl = "https://api.test.com/",
                ApiKey = "test-key"
            }
        };

        _settingsMock.Setup(s => s.Value).Returns(settings);
    }

    [Fact]
    public async Task GetExchangeRateAsync_WithSuccessfulResponse_ReturnsSuccess()
    {
        var response = new { rate = 85.5m };
        var responseContent = JsonSerializer.Serialize(response);

        _httpMessageHandlerMock.Protected()
   .Setup<Task<HttpResponseMessage>>(
       "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
       ItExpr.IsAny<CancellationToken>())
 .ReturnsAsync(new HttpResponseMessage
 {
     StatusCode = HttpStatusCode.OK,
     Content = new StringContent(responseContent)
 });

        var provider = new JsonExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(85.5m);
        result.Source.Should().Be("JsonAPI");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WithHttpError_ReturnsFailure()
    {
        _httpMessageHandlerMock.Protected()
   .Setup<Task<HttpResponseMessage>>(
     "SendAsync",
    ItExpr.IsAny<HttpRequestMessage>(),
          ItExpr.IsAny<CancellationToken>())
  .ReturnsAsync(new HttpResponseMessage
  {
      StatusCode = HttpStatusCode.InternalServerError,
      Content = new StringContent("Error")
  });

        var provider = new JsonExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("InternalServerError");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WithInvalidResponse_ReturnsFailure()
    {
        _httpMessageHandlerMock.Protected()
           .Setup<Task<HttpResponseMessage>>(
       "SendAsync",
             ItExpr.IsAny<HttpRequestMessage>(),
          ItExpr.IsAny<CancellationToken>())
         .ReturnsAsync(new HttpResponseMessage
         {
             StatusCode = HttpStatusCode.OK,
             Content = new StringContent("{\"invalid\":\"data\"}")
         });

        var provider = new JsonExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inválida");
    }

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        var provider = new JsonExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        provider.ProviderName.Should().Be("JsonAPI");
    }
}
