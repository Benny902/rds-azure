namespace RDS.BackEnd.Manager.Geolocation.Middleware;



// Set the correlation ID in the response header 
public class CorrelationIdResponseMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var values))
        {
            var correlationId = values.FirstOrDefault();

            if (!string.IsNullOrEmpty(correlationId))
            {
                context.Items["X-Correlation-ID"] = correlationId;
            }
        }

        // Set up the OnStarting callback
        context.Response.OnStarting(() =>
        {
           
            if (context.Items.TryGetValue("X-Correlation-ID", out var storedId) &&
                storedId != null)
            {
                context.Response.Headers["X-Correlation-ID"] = storedId.ToString();
            }
            return Task.CompletedTask;
        });

        await next(context);
    }
}