using System.Net;
using Dapr.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RDS.BackEnd.Manager.Geolocation.HealthChecks;

public class DaprServiceHealthCheck(DaprClient daprClient, string appId) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = daprClient.CreateInvokeMethodRequest(
                HttpMethod.Get,
                appId,
                "health"
            );

            var response = await daprClient.InvokeMethodWithResponseAsync(request, cancellationToken);

            return response.StatusCode == HttpStatusCode.OK
                ? HealthCheckResult.Healthy($"{appId} is healthy")
                : HealthCheckResult.Unhealthy($"{appId} returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"{appId} unreachable: {ex.Message}");
        }
    }
}
