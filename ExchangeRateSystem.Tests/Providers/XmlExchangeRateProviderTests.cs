using ExchangeRateSystem.Infrastructure.Configuration;
using ExchangeRateSystem.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using Xunit;

namespace ExchangeRateSystem.Tests.Providers;

public class XmlExchangeRateProviderTests
{
    private readonly Mock<ILogger<XmlExchangeRateProvider>> _loggerMock;
    private readonly Mock<IOptions<ExchangeRateApiSettings>> _settingsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;

    public XmlExchangeRateProviderTests()
    {
        _loggerMock = new Mock<ILogger<XmlExchangeRateProvider>>();
        _settingsMock = new Mock<IOptions<ExchangeRateApiSettings>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.test.com/")
        };

        var settings = new ExchangeRateApiSettings
        {
            XmlApi = new XmlApiSettings
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
        var responseXml = "<XML><Result>85.5</Result></XML>";

        _httpMessageHandlerMock.Protected()
              .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
               ItExpr.IsAny<HttpRequestMessage>(),
             ItExpr.IsAny<CancellationToken>())
                  .ReturnsAsync(new HttpResponseMessage
                  {
                      StatusCode = HttpStatusCode.OK,
                      Content = new StringContent(responseXml, System.Text.Encoding.UTF8, "application/xml")
                  });

        var provider = new XmlExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(85.5m);
        result.Source.Should().Be("XmlAPI");
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
            StatusCode = HttpStatusCode.BadRequest,
            Content = new StringContent("Error")
        });

        var provider = new XmlExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BadRequest");
    }

    [Fact]
    public async Task GetExchangeRateAsync_WithInvalidXml_ReturnsFailure()
    {
        var invalidXml = "<XML><Invalid>data</Invalid></XML>";

        _httpMessageHandlerMock.Protected()
               .Setup<Task<HttpResponseMessage>>(
           "SendAsync",
     ItExpr.IsAny<HttpRequestMessage>(),
       ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(new HttpResponseMessage
               {
                   StatusCode = HttpStatusCode.OK,
                   Content = new StringContent(invalidXml, System.Text.Encoding.UTF8, "application/xml")
               });

        var provider = new XmlExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        var result = await provider.GetExchangeRateAsync("USD", "EUR", 100);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("inválida");
    }

    [Fact]
    public void ProviderName_ReturnsCorrectName()
    {
        var provider = new XmlExchangeRateProvider(_httpClient, _settingsMock.Object, _loggerMock.Object);

        provider.ProviderName.Should().Be("XmlAPI");
    }
}
