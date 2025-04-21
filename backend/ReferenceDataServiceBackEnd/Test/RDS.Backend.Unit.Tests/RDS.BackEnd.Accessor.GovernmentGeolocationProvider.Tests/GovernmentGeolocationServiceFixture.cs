using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Configuration;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Tests;

public class GovernmentGeolocationServiceFixture : IDisposable
{
    public Mock<IGovernmentApiClient> MockApiClient { get; }
    public Mock<IGeolocationParser> MockParser { get; }
    private Mock<ILogger<GovernmentGeolocationService>> MockLogger { get; }
    private Mock<IOptionsMonitor<GovernmentApiOptions>> MockOptions { get; }
    private IConfiguration Configuration { get; }

    public GovernmentGeolocationService ServiceUnderTest { get; }

    public GovernmentGeolocationServiceFixture()
    {
        MockApiClient = new Mock<IGovernmentApiClient>();
        MockParser = new Mock<IGeolocationParser>();
        MockLogger = new Mock<ILogger<GovernmentGeolocationService>>();
        MockOptions = new Mock<IOptionsMonitor<GovernmentApiOptions>>();

        MockOptions.Setup(opt => opt.CurrentValue).Returns(new GovernmentApiOptions
        {
            LocalitiesEndpoint = "localities",
            StreetsEndpoint = "streets",
            BaseUrl = "https://data.gov.il/api/3/action/"
        });

        Configuration = new ConfigurationBuilder().Build();

        ServiceUnderTest = new GovernmentGeolocationService(
            MockApiClient.Object,
            MockParser.Object,
            Configuration,
            MockLogger.Object,
            MockOptions.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}