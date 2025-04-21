using RDS.BackEnd.Accessor.GeolocationInformation.Endpoints;
using RDS.BackEnd.Accessor.GeolocationInformation.Services;
using Microsoft.Azure.Cosmos;
using RDS.BackEnd.Accessor.GeolocationInformation.Middleware;
using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Accessor.GeolocationInformation;

var builder = WebApplication.CreateBuilder(args);

// Load CosmosDB Configuration from appsettings.json
var cosmosConfig = builder.Configuration.GetSection("CosmosDb");
var cosmosEndpoint = cosmosConfig["AccountEndpoint"];
var cosmosKey = cosmosConfig["AuthKey"];
var cosmosDatabase = cosmosConfig["DatabaseId"];
var cosmosContainer = cosmosConfig["ContainerId"];
var connectionMode = cosmosConfig["ConnectionMode"];

// Validate CosmosDB configuration
if (string.IsNullOrEmpty(cosmosEndpoint) || string.IsNullOrEmpty(cosmosKey) ||
    string.IsNullOrEmpty(cosmosDatabase) || string.IsNullOrEmpty(cosmosContainer))
{
    throw new InvalidOperationException("CosmosDB configuration is missing. Check appsettings.json.");
}


builder.Services.AddSingleton<CosmosClient>(_ =>
{
    var options = new CosmosClientOptions
    {
        ConnectionMode = connectionMode != null && connectionMode.Equals("direct", StringComparison.CurrentCultureIgnoreCase) ? ConnectionMode.Direct : ConnectionMode.Gateway,
        RequestTimeout = TimeSpan.FromSeconds(10),
        AllowBulkExecution = true 
    };

    return new CosmosClient(cosmosEndpoint, cosmosKey, options);
});

// Register IDatabaseService implementation (CosmosDbService)
builder.Services.AddSingleton<IDatabaseService, CosmosDbService>(); 

// Register CacheService
builder.Services.AddSingleton<ICacheService,CacheService>(); 

// Register GeolocationInformationService and its dependencies
builder.Services.AddSingleton<IGeolocationInformationService, GeolocationInformationService>(); // GeolocationInformationService that uses IDatabaseService and CacheService

// Register GeolocationInformationServiceInitializer to initialize the DB
builder.Services.AddHostedService<GeolocationInformationServiceInitializer>();

// Register other services like OpenAPI and Dapr
builder.Services.AddOpenApi();
builder.Services.AddDaprClient();

// Set up output caching (for caching responses)
var outputCacheExpiration = InternalConfiguration.Timeouts
    .First(kv => kv.Key == "OutputCacheExpiration").Value;
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policyBuilder => policyBuilder.Expire(outputCacheExpiration)); // Default cache expiration

    // Base policy for all localities
    options.AddBasePolicy(policyBuilder =>
        policyBuilder.With(c => c.HttpContext.Request.Path.StartsWithSegments("/v1/localities"))
            .Tag("localities"));

    options.AddPolicy("AllLocalitiesPolicy", policyBuilder =>
        policyBuilder.Tag("localities").Expire(outputCacheExpiration));

    options.AddPolicy("SingleLocalityPolicy", policyBuilder =>
        policyBuilder.SetVaryByQuery("id").Tag("localities").Expire(outputCacheExpiration));

    options.AddPolicy("StreetsPolicy", policyBuilder =>
        policyBuilder.SetVaryByQuery("id").Tag("streets").Expire(outputCacheExpiration)); // Cache per locality ID, 1 minutes
});

// Build and run the application
var app = builder.Build();

app.UseGlobalExceptionHandling();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}




app.UseOutputCache(); // Enable output caching

app.UseHttpsRedirection();

// Map the endpoints defined for geolocation information
app.MapGeolocationInformationEndpoints();



app.MapGet("/version", ([FromServices] ILogger<Program> logger) =>
{
    logger.LogInformation("Geolocation Information version");

    var commit = Environment.GetEnvironmentVariable("GIT_COMMIT") ?? "unknown";
    var buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? "unknown";
    var version = Environment.GetEnvironmentVariable("VERSION") ?? "unknown";



    return Task.FromResult(Results.Json(new
    {
        version,
        commit,
        buildTime,

    }));
});

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
