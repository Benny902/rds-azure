using System.Net;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RDS.BackEnd.Manager.Geolocation.Exceptions;
using RDS.BackEnd.Manager.Geolocation.Models;
using RDS.BackEnd.Manager.Geolocation.Helpers;
using RDS.BackEnd.Manager.Geolocation.Utils;

namespace RDS.BackEnd.Manager.Geolocation.Endpoints;
public static class GeolocationEndpoints
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public static void MapGeolocationEndpoints(this WebApplication app)
    {
        #region HttpGet
        
        app.MapGet("/v1/localities", async (
            HttpContext context,
            [FromServices] ILogger<Program> logger) =>
        {
            var dapr = context.RequestServices.GetRequiredService<DaprInvoker>();
            logger.LogDebug("Inside GetLocalities - Fetching localities.");

            var request = dapr.CreateRequest(HttpMethod.Get, "geolocation-information-accessor", "/localities");
            var httpResponse = await dapr.InvokeWithResponseAsync(request);

            if (!httpResponse.IsSuccessStatusCode)
            {
                var problem = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
                logger.LogWarning("Internal error: {Title} - {Detail}", problem?.Title, problem?.Detail);
                return Results.Problem(
                    title: "Internal server error",
                    detail: "Something went wrong. Please try again later.",
                    statusCode: 500
                );
            }
            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            var localities = JsonSerializer.Deserialize<List<LocalityDto>>(responseContent, JsonSerializerOptions);

            if (localities == null || localities.Count == 0)
                return Results.NotFound("No localities found.");

       
            context.Response.Headers.ETag = ETagHelper.GenerateETag(localities);

            return Results.Ok(localities);
        }).WithName("GetLocalities");

        app.MapGet("/v1/localities/{id}", async (
            HttpContext context,
            [FromRoute] string id,
            [FromServices] ILogger<Program> logger) =>
        {
            var dapr = context.RequestServices.GetRequiredService<DaprInvoker>();
            logger.LogDebug($"Inside {nameof(MapGeolocationEndpoints)} - Fetching locality by ID: {id}.");

            var request = dapr.CreateRequest(HttpMethod.Get, "geolocation-information-accessor", $"/localities/{id}");
            var httpResponse = await dapr.InvokeWithResponseAsync(request);
            
            if (!httpResponse.IsSuccessStatusCode)
            {
              
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    return Results.NotFound($"Locality with ID {id} not found.");
                }
                var problem = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
                logger.LogWarning("Internal error: {Title} - {Detail}", problem?.Title, problem?.Detail);
                return Results.Problem(
                    title: "Internal server error",
                    detail: "Something went wrong. Please try again later.",
                    statusCode: 500
                );
            }

            var content = await httpResponse.Content.ReadAsStringAsync();

            var locality = JsonSerializer.Deserialize<LocalityDto>(content, JsonSerializerOptions);

            if (locality == null)
                return Results.NotFound($"Locality with ID {id} not found.");

            logger.LogDebug($"Successfully retrieved locality {id}.");

     
            context.Response.Headers.ETag = ETagHelper.GenerateETag(locality);

            return Results.Ok(locality);
        }).WithName("GetLocalityById");

        app.MapGet("/v1/localities/{id}/streets", async (
            HttpContext context,
            [FromRoute] string id,
            [FromServices] ILogger<Program> logger) =>
        {
            var dapr = context.RequestServices.GetRequiredService<DaprInvoker>();
            logger.LogDebug($"Inside GetStreetsByLocalityId- Fetching streets for locality {id}.");

            var request = dapr.CreateRequest(HttpMethod.Get, "geolocation-information-accessor", $"/localities/{id}/streets");
            var httpResponse = await dapr.InvokeWithResponseAsync(request);

            if (!httpResponse.IsSuccessStatusCode)
            {
                if (httpResponse.StatusCode is HttpStatusCode.NotFound)
                    return Results.NotFound($"No streets found for locality {id}.");

                var problem = await httpResponse.Content.ReadFromJsonAsync<ProblemDetails>();
                logger.LogWarning("Internal error: {Title} - {Detail}", problem?.Title, problem?.Detail);
                return Results.Problem(
                    title: "Internal server error",
                    detail: "Something went wrong. Please try again later.",
                    statusCode: 500
                );
            }

            var responseContent = await httpResponse.Content.ReadAsStringAsync();
            var streets = JsonSerializer.Deserialize<List<StreetDto>>(responseContent, JsonSerializerOptions);

            if (streets == null || streets.Count == 0)
                return Results.NotFound($"No streets found for locality {id}.");

            logger.LogDebug($"Successfully retrieved streets for locality {id}.");
            
            context.Response.Headers.ETag = ETagHelper.GenerateETag(streets);

            return Results.Ok(streets);
        }).WithName("GetStreetsByLocalityId");

        #endregion

        #region HttpPost

        app.MapPost("/v1/localities", async (
            HttpContext context,
            [FromServices] ILogger<Program> logger,
            [FromServices] DaprInvoker dapr) =>
        {
            logger.LogInformation("Inside PostLocalities - Fetching latest data from Government API.");

            try
            {
                var govData = await dapr.InvokeGetAsync<List<Locality>>("government-geolocation-provider-accessor", "/geolocation");

                if (govData == null || govData.Count == 0)
                {
                    logger.LogWarning("Government provider returned no locality data.");
                    return Results.Problem("Government provider unavailable or returned no data.", statusCode: 503);
                }

                await dapr.InvokePostAsync("geolocation-information-accessor", "/localities", govData);
            }
            catch (UpsertFailedException ex)
            {
                logger.LogError(ex, "Failed to upsert some locality data.");
                return Results.Problem("Failed to upsert some locality data.", statusCode: 400);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Locality not found in government provider.");
                return Results.NotFound("No Locality data available.");
            }
            catch (ApplicationException ex)
            {
                logger.LogError(ex, "Failed to post locality data.");
                return Results.Problem("Internal server error", statusCode: 500);
            }

            logger.LogInformation("Successfully updated localities.");
            return Results.Ok();
        }).WithName("PostLocalities");

        app.MapPost("/v1/streets", async (
            HttpContext context,
            [FromServices] ILogger<Program> logger,
            [FromServices] DaprInvoker dapr) =>
        {
            var updateTimestamp = DateTime.UtcNow;

            logger.LogInformation("Inside PostStreets - Fetching updated street data from Government API.");

            try
            {
                var govStreetData = await dapr.InvokeGetAsync<List<Street>>(
                    "government-geolocation-provider-accessor",
                    "/streets"
                );

                if (govStreetData == null || govStreetData.Count == 0)
                {
                    logger.LogWarning("Government provider returned no street data.");
                    return Results.Problem("Government provider unavailable or returned no data.", statusCode: 503);
                }

                logger.LogInformation("Total streets retrieved: {Count}.", govStreetData.Count);

                var batchSize = int.Parse(
               InternalConfiguration.Default.First(kv => kv.Key == "StreetUpsertBatchSize").Value,
               CultureInfo.InvariantCulture
               );

                var batches = govStreetData
                    .Where(s => !string.IsNullOrEmpty(s.localityId))
                    .Chunk(batchSize)
                    .ToList();

                for (var i = 0; i < batches.Count; i++)
                {
                    var batch = batches[i];
                    var payload = new StreetBatchRequest
                    {
                        Streets = batch.ToList(),
                        UpdateTimestamp = updateTimestamp
                    };

                    logger.LogDebug("Sending Street batch {Index}/{Total} with {Count} items", i + 1, batches.Count, batch.Length);

                    await dapr.InvokePostAsync("geolocation-information-accessor", "/streets", payload);
                }

                logger.LogInformation("Successfully updated all street batches.");
                return Results.Ok();
            }catch (UpsertFailedException ex)
            {
                logger.LogError(ex, "Failed to upsert some street data.");
                return Results.Problem("Failed to upsert some street data.", statusCode: 400);
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Streets not found in government provider.");
                return Results.NotFound("No street data available.");
            }
            catch (ApplicationException ex)
            {
                logger.LogError(ex, "Error retrieving or posting street data.");
                return Results.Problem("Internal error processing street data.", statusCode: 500);
            }
        });



        #endregion
    }

}