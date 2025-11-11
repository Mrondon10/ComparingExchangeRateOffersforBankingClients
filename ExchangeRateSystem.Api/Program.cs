using ExchangeRateSystem.Core.Interfaces;
using ExchangeRateSystem.Core.Services;
using ExchangeRateSystem.Infrastructure.Configuration;
using ExchangeRateSystem.Infrastructure.Providers;
using Microsoft.AspNetCore.Mvc.Formatters;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.InputFormatters.Add(new XmlSerializerInputFormatter(options));
    options.OutputFormatters.Add(new XmlSerializerOutputFormatter());
});

builder.Services.Configure<ExchangeRateApiSettings>(
    builder.Configuration.GetSection(ExchangeRateApiSettings.SectionName));

var useMockProviders = builder.Configuration.GetValue<bool>("UseMockProviders", true);

if (useMockProviders)
{
    builder.Services.AddTransient<IExchangeRateProvider, MockJsonExchangeRateProvider>();
    builder.Services.AddTransient<IExchangeRateProvider, MockXmlExchangeRateProvider>();
    builder.Services.AddTransient<IExchangeRateProvider, MockJsonApi2ExchangeRateProvider>();
}
else
{
    builder.Services.AddHttpClient<JsonExchangeRateProvider>();
    builder.Services.AddHttpClient<XmlExchangeRateProvider>();
    builder.Services.AddHttpClient<JsonApi2ExchangeRateProvider>();

    builder.Services.AddTransient<IExchangeRateProvider, JsonExchangeRateProvider>();
    builder.Services.AddTransient<IExchangeRateProvider, XmlExchangeRateProvider>();
    builder.Services.AddTransient<IExchangeRateProvider, JsonApi2ExchangeRateProvider>();
}

builder.Services.AddScoped<IExchangeRateService, ExchangeRateService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Exchange Rate System API",
        Version = "v1",
        Description = "API for comparing exchange rate offers from multiple providers"
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Rate System API v1");
    c.RoutePrefix = string.Empty;
    c.DocumentTitle = "Exchange Rate System - API Documentation";
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
