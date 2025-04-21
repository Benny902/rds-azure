namespace RDS.BackEnd.Accessor.GeolocationInformation.Services;

public class GeolocationInformationServiceInitializer(
    IGeolocationInformationService geolocationInformationService,
    ILogger<GeolocationInformationServiceInitializer> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("{class} ,{method}: Starting Geolocation Information Service Initialization",nameof(GeolocationInformationServiceInitializer) ,nameof(StartAsync));
        try
        {
            await geolocationInformationService.InitializeAsync(cancellationToken);
            logger.LogInformation("Geolocation Information Service Initialization Completed");
            
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{class} ,{method}: Geolocation Information Service Initialization Failed",nameof(GeolocationInformationServiceInitializer) ,nameof(StartAsync));
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}