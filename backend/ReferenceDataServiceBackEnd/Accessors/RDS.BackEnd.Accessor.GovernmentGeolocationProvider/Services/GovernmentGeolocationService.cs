using Microsoft.Extensions.Options;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Configuration;
using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Models;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services;

public class GovernmentGeolocationService : IGovernmentGeolocationService
{
    private readonly IGovernmentApiClient _apiClient;
    private readonly IGeolocationParser _parser;
    private readonly ILogger<GovernmentGeolocationService> _logger;
    private readonly IOptionsMonitor<GovernmentApiOptions> _optionsMonitor;

    public GovernmentGeolocationService(
        IGovernmentApiClient apiClient,
        IGeolocationParser parser,
        IConfiguration config,
        ILogger<GovernmentGeolocationService> logger,
        IOptionsMonitor<GovernmentApiOptions> optionsMonitor)
    {
        _apiClient = apiClient;
        _parser = parser;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }


    public async Task<List<Locality>> FetchGeolocationDataAsync(CancellationToken cancellationToken)
    {
        // Always get the current value from options monitor
        var currentOptions = _optionsMonitor.CurrentValue;

        _logger.LogInformation("Fetching localities with endpoint: {Endpoint}",
            currentOptions.LocalitiesEndpoint);

        var response = await _apiClient.GetRawDataAsync(currentOptions.LocalitiesEndpoint, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response))
            return _parser.ParseLocalities(response);

        _logger.LogWarning("Empty locality response received.");
        return [];
    }

    public async Task<List<Street>> FetchStreetDataAsync(CancellationToken cancellationToken)
    {
        // Always get the current value from options monitor
        var currentOptions = _optionsMonitor.CurrentValue;

        _logger.LogInformation("Fetching streets with endpoint: {Endpoint}",
            currentOptions.StreetsEndpoint);

        var response = await _apiClient.GetRawDataAsync(currentOptions.StreetsEndpoint, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response))
            return _parser.ParseStreets(response);

        _logger.LogWarning("Empty street response received.");
        return [];
    }
}

