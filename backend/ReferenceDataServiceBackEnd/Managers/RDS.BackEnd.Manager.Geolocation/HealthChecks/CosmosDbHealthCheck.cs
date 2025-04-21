using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace RDS.BackEnd.Manager.Geolocation.HealthChecks
{
    public class CosmosDbHealthCheck(IConfiguration configuration) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = new CosmosClient(
                    configuration["CosmosDb:AccountEndpoint"],
                    configuration["CosmosDb:AuthKey"],
                    new CosmosClientOptions
                    {
                        ConnectionMode = ConnectionMode.Gateway
                    });

                var database = client.GetDatabase(configuration["CosmosDb:DatabaseId"]);
                var container = database.GetContainer(configuration["CosmosDb:ContainerId"]);

                var iterator = container.GetItemQueryIterator<dynamic>("SELECT * FROM c OFFSET 0 LIMIT 1");
                await iterator.ReadNextAsync(cancellationToken); // async, non-blocking

                return HealthCheckResult.Healthy("CosmosDB is reachable.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("CosmosDB is not reachable.", ex);
            }
        }
    }
}
