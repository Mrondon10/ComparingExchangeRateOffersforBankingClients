namespace ExchangeRateSystem.Core.Models;

public class ExchangeRateResponse
{
    public string StatusCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public ExchangeRateData? Data { get; set; }
}

public class ExchangeRateData
{
    public string SourceCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal ConvertedAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
