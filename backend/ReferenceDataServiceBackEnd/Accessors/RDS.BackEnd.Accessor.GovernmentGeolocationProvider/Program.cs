using System.Net;
using Microsoft.Extensions.Options;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Endpoints;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services;
using Polly;
using Polly.Extensions.Http;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Configuration;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Middleware;
using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddDaprClient();

// Enable configuration reloading
builder.Configuration.AddJsonFile("appsettings.json",
    optional: false,
    reloadOnChange: true);

// Register parser and service
builder.Services.AddScoped<IGeolocationParser, GeolocationParser>();
builder.Services.AddScoped<IGovernmentGeolocationService, GovernmentGeolocationService>();

// Configure options with reloading support
builder.Services.Configure<GovernmentApiOptions>(
    builder.Configuration.GetSection("GovernmentApi"));

// Add options monitor for dynamic updates
builder.Services.AddOptions<GovernmentApiOptions>()
    .Bind(builder.Configuration.GetSection("GovernmentApi"))
    .Configure<ILogger<GovernmentApiOptions>>((settings, logger) =>
    {
        logger.LogInformation("Configuring GovernmentApiOptions: BaseUrl={BaseUrl}, LocalitiesEndpoint={LocalitiesEndpoint}",
            settings.BaseUrl, settings.LocalitiesEndpoint);
    });

// Register typed HttpClient with Polly retry policy and dynamic base address
builder.Services.AddHttpClient<IGovernmentApiClient, GovernmentApiClient>((serviceProvider, client) =>
{
    // Use IOptionsMonitor to get the most recent configuration
    var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<GovernmentApiOptions>>();
    var options = optionsMonitor.CurrentValue;

    client.BaseAddress = new Uri(options.BaseUrl);

    // Optional: Subscribe to configuration changes
    optionsMonitor.OnChange((newOptions, _) =>
    {
        var logger = serviceProvider.GetRequiredService<ILogger<GovernmentApiClient>>();
        logger.LogInformation("HttpClient BaseAddress updated to: {BaseUrl}", newOptions.BaseUrl);
        client.BaseAddress = new Uri(newOptions.BaseUrl);
    });
})
.AddPolicyHandler(Policy<HttpResponseMessage>
    .HandleResult(resp => resp.StatusCode == HttpStatusCode.OK && resp.Content.Headers.ContentLength == 0)
    .OrTransientHttpError()
    .WaitAndRetryAsync(
        int.Parse(InternalConfiguration.Default.First(kv => kv.Key == "RetryCount").Value, CultureInfo.InvariantCulture),
        retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) +
            TimeSpan.FromMilliseconds(Random.Shared.Next(
                0,
                int.Parse(InternalConfiguration.Default.First(kv => kv.Key == "RetryJitterMillisecondsMax").Value, CultureInfo.InvariantCulture)
            ))
    )
);

// Optional: Add configuration monitoring service
builder.Services.AddSingleton<IHostedService, ConfigurationChangeMonitorService>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Register endpoints
app.MapGovernmentGeolocationEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));


app.MapGet("/version", ([FromServices] ILogger<Program> logger) =>
{
    logger.LogInformation("Government Geolocation version");

    var commit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown";
    var buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown";
    var version = Environment.GetEnvironmentVariable("VERSION") ?? "unknown";



    return Results.Json(new
    {
        version,
        commit,
        buildTime,

    });
});


app.Run();

// Optional: Hosted Service to monitor configuration changes
public class ConfigurationChangeMonitorService(
    IOptionsMonitor<GovernmentApiOptions> optionsMonitor,
    ILogger<ConfigurationChangeMonitorService> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Subscribe to configuration changes
        optionsMonitor.OnChange((options, name) =>
        {
            logger.LogInformation("Configuration changed for {Name}. New BaseUrl: {BaseUrl}, New LocalitiesEndpoint: {LocalitiesEndpoint}",
                name, options.BaseUrl, options.LocalitiesEndpoint);
        });

        return Task.CompletedTask;
    }
}