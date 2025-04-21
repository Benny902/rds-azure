using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using RDS.BackEnd.Accessor.GeolocationInformation.Exceptions;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Middleware;

public class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BulkUpsertFailedException ex)
        {
            logger.LogWarning(ex, "Bulk upsert failed for entity type '{EntityType}' at {Path}", ex.EntityType, context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = $"Some {ex.EntityType} failed to upsert.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = context.Request.Path,
                Extensions =
                {
                    { $"failed{ex.EntityType}Ids", ex.FailedIds }
                }
            });
        }

        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Not Found Exception at {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Resource not found",
                Detail = ex.Message,
                Status = StatusCodes.Status404NotFound,
                Instance = context.Request.Path
            });
        }
        catch (CosmosException ex)
        {
            logger.LogError(ex, "CosmosDB exception at {Path}", context.Request.Path);

            context.Response.StatusCode = (int)ex.StatusCode;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Cosmos DB Error",
                Detail = ex.Message,
                Status = (int)ex.StatusCode,
                Instance = context.Request.Path
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Title = "Unexpected error occurred",
                Detail = ex.Message,
                Status = StatusCodes.Status500InternalServerError,
                Instance = context.Request.Path
            });
        }
    }
}
