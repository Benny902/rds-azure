using Microsoft.Extensions.Options;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Configuration;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public class GovernmentApiConfigurationMonitor
    {
        private readonly IOptionsMonitor<GovernmentApiOptions> _optionsMonitor;
        private readonly ILogger<GovernmentApiConfigurationMonitor> _logger;

        public GovernmentApiConfigurationMonitor(
            IOptionsMonitor<GovernmentApiOptions> optionsMonitor,
            ILogger<GovernmentApiConfigurationMonitor> logger)
        {
            _optionsMonitor = optionsMonitor;
            _logger = logger;

            // Subscribe to configuration changes
            _optionsMonitor.OnChange(OnConfigurationChanged);
        }

        private void OnConfigurationChanged(GovernmentApiOptions options, string? name)
        {
            _logger.LogInformation("Government API Configuration changed. New Localities Endpoint: {Endpoint}",
                options.LocalitiesEndpoint);
        }
    }
}
