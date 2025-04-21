using System.Text.Json;

namespace RDS.BackEnd.Manager.Geolocation.Middleware;



// Run before the endpoint to validate the correlation ID is in the request header for the specified paths
public class CorrelationIdMiddleware(RequestDelegate next)
{
    private static readonly string[] RequiredPaths =
   {  
        "/v1/localities",
        "/v1/localities/",
        "/v1/streets"
    };
    public async Task InvokeAsync(HttpContext context)
    {

        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

     
        var shouldValidate = RequiredPaths.Any(required =>
            path.StartsWith(required, StringComparison.OrdinalIgnoreCase));

        if (shouldValidate)
        {
            if (!context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) ||
                string.IsNullOrWhiteSpace(correlationId) ||
                !Guid.TryParse(correlationId, out _))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(new { error = "Invalid or missing Correlation ID" });
                await context.Response.WriteAsync(json);
                return;
            }

        }

        await next(context);
    }
}
