namespace RDS.BackEnd.Accessor.GeolocationInformation.Test;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using Services;

public class GeolocationInformationServiceFixture : IDisposable
{
    public Mock<IDatabaseService> MockDatabaseService { get; }
    public Mock<ICacheService> MockCacheService { get; }
    private Mock<ILogger<GeolocationInformationService>> MockLogger { get; }
    private Mock<ILogger<CosmosDbService>> MockCosmosLogger { get; }
    private Mock<CosmosClient> MockCosmosClient { get; }
    private IConfiguration Configuration { get; }
    public GeolocationInformationService GeolocationService { get; }
    public CosmosDbService CosmosDbService { get; }

    public GeolocationInformationServiceFixture()
    {
        // Mocks
        MockDatabaseService = new Mock<IDatabaseService>();
        MockCacheService = new Mock<ICacheService>();
        MockLogger = new Mock<ILogger<GeolocationInformationService>>();
        MockCosmosLogger = new Mock<ILogger<CosmosDbService>>();
        MockCosmosClient = new Mock<CosmosClient>();

        // Config
        Configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "CosmosDb:DatabaseId", "test-db" },
                { "CosmosDb:ContainerId", "localities" },
                { "CosmosDb:StreetsContainerId", "streets" }
            }!)
            .Build();

        // Services
        CosmosDbService = new CosmosDbService(MockCosmosClient.Object, Configuration, MockCosmosLogger.Object);
        GeolocationService = new GeolocationInformationService(
            MockDatabaseService.Object,
            MockCacheService.Object,
            MockLogger.Object
        );

        // Reset static field for clean tests
        typeof(GeolocationInformationService)
            .GetField("_lastStreetUpdateTime",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(null, DateTime.MinValue);
    }

    public void Dispose()
    {
        // clean up if needed
    }
}
