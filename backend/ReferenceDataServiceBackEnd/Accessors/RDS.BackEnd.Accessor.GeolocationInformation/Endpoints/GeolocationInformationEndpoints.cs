using RDS.BackEnd.Accessor.GeolocationInformation.Services;
using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Accessor.GeolocationInformation.Models;
using System.Text;
using System.Text.Json;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Endpoints;

public static class GeolocationInformationEndpoints
{
    public static void MapGeolocationInformationEndpoints(this WebApplication app)
    {
        #region HttpGet

        app.MapGet("/localities", async (HttpContext context, [FromServices] IGeolocationInformationService service,
                [FromServices] ILogger<Program> logger) =>
            {
                logger.LogDebug($"GeolocationInformationEndpoints: Inside GetALlLocalities - Fetching all localities.");


                var data = await service.GetLocalitiesAsync(context.RequestAborted);
                if (data.Count == 0)
                {
                    logger.LogWarning($"{nameof(MapGeolocationInformationEndpoints)}: No localities found.");
                    return Results.NotFound("No localities found.");
                }

                logger.LogDebug($"{nameof(MapGeolocationInformationEndpoints)}: Successfully retrieved localities.");
                var serialized = JsonSerializer.Serialize(data);
                var hash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(serialized))
                ).Substring(0, 16); // Take first 16 chars for brevity
                context.Response.Headers.ETag = $"\"{hash}\"";

                return Results.Ok(data);
            }).CacheOutput("AllLocalitiesPolicy")
            .WithTags("localities").WithName("GetALlLocalities");

        app.MapGet("/localities/{id}",
            async (HttpContext context, [FromRoute] string id, [FromServices] IGeolocationInformationService service,
                [FromServices] ILogger<Program> logger) =>
            {
                logger.LogInformation(
                    "GeolocationInformationEndpoints: Inside GetLocalityById - Fetching locality with ID {id}.",
                    id);

                var data = await service.GetLocalityByIdAsync(id, context.RequestAborted);
                if (data == null)
                {
                    logger.LogWarning(
                        "GetLocalityById: Locality with ID {id} not found.", id);
                    return Results.NotFound($"Locality with ID {id} not found.");
                }

                logger.LogInformation(
                    "GetLocalityById : Successfully retrieved locality with ID {id}.", id);


                var serialized = JsonSerializer.Serialize(data);
                var hash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(serialized))
                ).Substring(0, 16); // Take first 16 chars for brevity
                context.Response.Headers.ETag = $"\"{hash}\"";
                return Results.Ok(data);
            }).CacheOutput("SingleLocalityPolicy").WithTags("localities").WithName("GetLocalityById");

        app.MapGet("/localities/{id}/streets", async (HttpContext context, [FromRoute] string id,
            [FromServices] IGeolocationInformationService service, [FromServices] ILogger<Program> logger) =>
        {
            logger.LogDebug(
                "GeolocationInformationEndpoints: Inside GetStreetsByLocalityId - Fetching streets for locality ID {id}.",
                id);

            var streets = await service.GetStreetsByLocalityAsync(id, context.RequestAborted);
            if (streets.Count == 0)
            {
                logger.LogWarning($"GetStreetsByLocalityId: No streets found for locality ID {id}.");
                return Results.NotFound($"No streets found for locality ID {id}.");
            }

   
            var serialized = JsonSerializer.Serialize(streets);
            var hash = Convert.ToBase64String(
                System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(serialized))
            ).Substring(0, 16); // Take first 16 chars for brevity
            context.Response.Headers.ETag = $"\"{hash}\"";

            logger.LogDebug(
                "GetStreetsByLocalityId: Successfully retrieved {streets.Count} streets for locality ID {id}.",
                streets.Count, id);
            return Results.Ok(streets);
        }).CacheOutput("StreetsPolicy").WithTags("streets").WithName("GetStreetsByLocalityId");

        #endregion

        #region HttpPost

        app.MapPost("/localities", async (HttpContext context, [FromBody] List<Locality> localities,
            [FromServices] IGeolocationInformationService service, [FromServices] ILogger<Program> logger) =>
        {
            logger.LogDebug(
                $"GeolocationInformationEndpoints: Inside MapPost-UpsertLocalitiesAsync - Updating localities with received data.");

            await service.UpsertLocalitiesAsync(localities, context.RequestAborted);

            logger.LogDebug("MapPost-UpsertLocalitiesAsync: Successfully updated localities.");

            return Results.Ok();
        }).WithName("MapPostUpsertLocalitiesAsync");

        app.MapPost("/streets",
            async (HttpContext context, [FromBody] StreetBatchRequest request,
                [FromServices] IGeolocationInformationService service, [FromServices] ILogger<Program> logger) =>
            {
                logger.LogDebug(
                    $"GeolocationInformationEndpoints: Inside MapPostUpsertStreetDataAsync - Updating streets with received data.");

                await service.UpsertStreetDataAsync(request, context.RequestAborted);


                logger.LogDebug("MapPost-UpsertStreetDataAsync: Successfully updated streets.");
                return Results.Ok();
            }).WithName("MapPostUpsertStreetDataAsync");

        #endregion
    }
}