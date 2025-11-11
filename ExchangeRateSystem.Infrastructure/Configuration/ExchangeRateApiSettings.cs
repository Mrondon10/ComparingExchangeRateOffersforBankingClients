namespace ExchangeRateSystem.Infrastructure.Configuration;

public class ExchangeRateApiSettings
{
    public const string SectionName = "ExchangeRateApis";

    public JsonApiSettings JsonApi { get; set; } = new();
    public XmlApiSettings XmlApi { get; set; } = new();
    public JsonApi2Settings JsonApi2 { get; set; } = new();
}

public class JsonApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class XmlApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class JsonApi2Settings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
