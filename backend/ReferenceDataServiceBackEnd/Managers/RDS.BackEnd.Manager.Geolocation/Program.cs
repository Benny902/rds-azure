using System.Text.Json.Nodes;
using Dapr.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RDS.BackEnd.Manager.Geolocation.Endpoints;
using RDS.BackEnd.Manager.Geolocation.HealthChecks;
using RDS.BackEnd.Manager.Geolocation.Helpers;
using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Manager.Geolocation.Middleware;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDaprClient();
builder.Services.AddScoped<DaprInvoker>();

// Register Accessors HealthChecks
builder.Services.AddSingleton<IHealthCheck>(sp =>
    new DaprServiceHealthCheck(sp.GetRequiredService<DaprClient>(), "government-geolocation-provider-accessor"));

builder.Services.AddSingleton<IHealthCheck>(sp =>
    new DaprServiceHealthCheck(sp.GetRequiredService<DaprClient>(), "geolocation-information-accessor"));


// Read configuration
var configuration = builder.Configuration;

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("geolocation-manager", () => HealthCheckResult.Healthy(), tags: ["liveness"])
    .Add(new HealthCheckRegistration(
        "government-accessor",
        sp => new DaprServiceHealthCheck(sp.GetRequiredService<DaprClient>(), "government-geolocation-provider-accessor"),
        HealthStatus.Unhealthy,
        tags: ["readiness"]))
    .Add(new HealthCheckRegistration(
        "information-accessor",
        sp => new DaprServiceHealthCheck(sp.GetRequiredService<DaprClient>(), "geolocation-information-accessor"),
        HealthStatus.Unhealthy,
        tags: ["readiness"]))
    .AddCheck<CosmosDbHealthCheck>("CosmosDB", tags: ["readiness"]);

// Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GeolocationManager"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://zipkin:9411/api/v2/spans");
            });
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("GeolocationManager"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });


var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<CorrelationIdResponseMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGeolocationEndpoints();



// Add Middleware for Health Checks
app.UseHealthChecks("/health", new HealthCheckOptions
{
    
    ResponseWriter = async (context, report) =>
    {
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() }),
            duration = report.TotalDuration.TotalMilliseconds
        });
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(result);
    }
});

app.UseHealthChecks("/liveness", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("liveness")
});

app.UseHealthChecks("/readiness", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("readiness")
});


app.MapGet("/version", async ([FromServices] ILogger<Program> logger, [FromServices] DaprInvoker invoker) =>
{
    logger.LogInformation("Geolocation endpoint version");

    var commit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown";
    var buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown";
    var version = Environment.GetEnvironmentVariable("VERSION") ?? "unknown";

    var geolocationVersion = new
    {
        version,
        commit,
        buildTime,

    };

    var geolocationInformationVersion = await invoker.InvokeAsync<JsonNode>(
        HttpMethod.Get,
        "geolocation-information-accessor",
        "/version"
    );

    var governmentGeolocationProviderVersion = await invoker.InvokeAsync<JsonNode>(
        HttpMethod.Get,
        "government-geolocation-provider-accessor",
        "/version"
    );


    return Results.Json(new
    {
        manager = geolocationVersion,
        geolocationInformationAccessorVersion = geolocationInformationVersion,
        governmentGeolocationProviderAccessorVersion = governmentGeolocationProviderVersion
    });
});


app.Run();