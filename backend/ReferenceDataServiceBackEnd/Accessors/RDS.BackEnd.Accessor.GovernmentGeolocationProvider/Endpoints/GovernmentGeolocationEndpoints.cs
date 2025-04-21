using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Endpoints;

public static class GovernmentGeolocationEndpoints
{
    public static void MapGovernmentGeolocationEndpoints(this WebApplication app)
    {
        app.MapGet("/geolocation", async ([FromServices] IGovernmentGeolocationService service,
            [FromServices] ILogger<Program> logger,
            CancellationToken cancellationToken
        ) =>
        {

            logger.LogDebug(
                $"GovernmentGeolocationEndpoints: Inside {nameof(MapGovernmentGeolocationEndpoints)} - Fetching government geolocation data.");
          
            var data = await service.FetchGeolocationDataAsync(cancellationToken);
            if (data.Count == 0)
            {
                logger.LogWarning("Government API returned no geolocation data.");
                return Results.NotFound();
            }

            logger.LogDebug($"{nameof(MapGovernmentGeolocationEndpoints)}: Successfully fetched geolocation data.");
            return Results.Ok(data);
        }).WithName("GetGeolocation");

        app.MapGet("/streets", async ([FromServices] IGovernmentGeolocationService service,
            [FromServices] ILogger<Program> logger, CancellationToken cancellationToken) =>
        {

            logger.LogDebug(
                $"GovernmentGeolocationEndpoints: Inside {nameof(MapGovernmentGeolocationEndpoints)} - Fetching streets data.");

            var data = await service.FetchStreetDataAsync(cancellationToken);
            if (data.Count == 0)
            {
                logger.LogWarning("Government API returned no street data.");
                return Results.NotFound();
            }

            logger.LogDebug($"{nameof(MapGovernmentGeolocationEndpoints)}: Successfully fetched street data.");
            return Results.Ok(data);
        }).WithName("GetStreets");
    }
}