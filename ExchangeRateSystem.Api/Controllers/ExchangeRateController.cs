using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;

namespace ExchangeRateSystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExchangeRateController : ControllerBase
{
    private readonly IExchangeRateService _exchangeRateService;
    private readonly ILogger<ExchangeRateController> _logger;

    public ExchangeRateController(
        IExchangeRateService exchangeRateService,
        ILogger<ExchangeRateController> logger)
    {
        _exchangeRateService = exchangeRateService ?? throw new ArgumentNullException(nameof(exchangeRateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("json")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> GetExchangeRateJson([FromBody] JsonRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("JSON API called with {From} -> {To}, Value: {Value}",
          request.From, request.To, request.Value);

            var exchangeRequest = new ExchangeRateRequest
            {
                SourceCurrency = request.From,
                TargetCurrency = request.To,
                Amount = request.Value
            };

            var result = await _exchangeRateService.GetBestExchangeRateAsync(exchangeRequest, cancellationToken);

            if (result.StatusCode != "200")
            {
                return StatusCode(500, new { rate = 0m, error = result.Message });
            }

            return Ok(new { rate = result.Data!.ConvertedAmount });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new { rate = 0m, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing JSON request");
            return StatusCode(500, new { rate = 0m, error = "Error interno del servidor" });
        }
    }

    [HttpPost("xml")]
    [Consumes("application/xml", "text/xml")]
    [Produces("application/xml", "text/xml")]
    public async Task<IActionResult> GetExchangeRateXml([FromBody] XmlRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("XML API called with {From} -> {To}, Amount: {Amount}",
        request.From, request.To, request.Amount);

            var exchangeRequest = new ExchangeRateRequest
            {
                SourceCurrency = request.From,
                TargetCurrency = request.To,
                Amount = request.Amount
            };

            var result = await _exchangeRateService.GetBestExchangeRateAsync(exchangeRequest, cancellationToken);

            if (result.StatusCode != "200")
            {
                var errorXml = new XElement("XML",
                        new XElement("Result", 0),
                  new XElement("Error", result.Message)
                               );
                return Content(errorXml.ToString(), "application/xml");
            }

            var responseXml = new XElement("XML",
      new XElement("Result", result.Data!.ConvertedAmount)
            );

            return Content(responseXml.ToString(), "application/xml");
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            var errorXml = new XElement("XML",
        new XElement("Result", 0),
           new XElement("Error", ex.Message)
               );
            return BadRequest(Content(errorXml.ToString(), "application/xml"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing XML request");
            var errorXml = new XElement("XML",
                       new XElement("Result", 0),
              new XElement("Error", "Error interno del servidor")
              );
            return StatusCode(500, Content(errorXml.ToString(), "application/xml"));
        }
    }

    [HttpPost("exchange")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> GetExchange([FromBody] ExchangeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Exchange API called with {From} -> {To}, Quantity: {Quantity}",
                       request.Exchange.SourceCurrency, request.Exchange.TargetCurrency, request.Exchange.Quantity);

            var exchangeRequest = new ExchangeRateRequest
            {
                SourceCurrency = request.Exchange.SourceCurrency,
                TargetCurrency = request.Exchange.TargetCurrency,
                Amount = request.Exchange.Quantity
            };

            var result = await _exchangeRateService.GetBestExchangeRateAsync(exchangeRequest, cancellationToken);

            return Ok(new
            {
                statusCode = result.StatusCode,
                message = result.Message,
                data = result.Data != null ? new { total = result.Data.ConvertedAmount } : null
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request parameters");
            return BadRequest(new
            {
                statusCode = "400",
                message = ex.Message,
                data = (object?)null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing exchange request");
            return StatusCode(500, new
            {
                statusCode = "500",
                message = "Error interno del servidor",
                data = (object?)null
            });
        }
    }

    public record JsonRequest(string From, string To, decimal Value);

    public record XmlRequest
    {
        public string From { get; set; } = string.Empty;
        public string To { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public record ExchangeRequest(ExchangeData Exchange);
    public record ExchangeData(string SourceCurrency, string TargetCurrency, decimal Quantity);
}
