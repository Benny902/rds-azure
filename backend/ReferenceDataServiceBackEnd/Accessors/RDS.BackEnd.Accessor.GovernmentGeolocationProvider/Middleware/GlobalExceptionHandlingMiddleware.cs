namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Middleware
{
    public class GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await next(context); // continue the pipeline
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, "Not Found Exception at {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new
                {
                    Title = "An error occurred while processing your request.",
                    Detail = ex.Message
                });
            }
        }
    }
}