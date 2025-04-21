using RDS.BackEnd.Accessor.GeolocationInformation.Models;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Services
{
    public class GeolocationInformationService : IGeolocationInformationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GeolocationInformationService> _logger;
        private static DateTime _lastStreetUpdateTime = DateTime.MinValue; // Store the last update time

        public GeolocationInformationService(IDatabaseService databaseService, ICacheService cacheService, ILogger<GeolocationInformationService> logger)
        {
            _databaseService = databaseService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Initializing Geolocation Information Service. In {method} ", nameof(InitializeAsync));
            try
            {
                await _databaseService.InitializeAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Geolocation Information Service. In {method} ", nameof(InitializeAsync));
                throw;
            }
        }

        public async Task<List<LocalityDto>> GetLocalitiesAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching localities. In {method} ", nameof(GetLocalitiesAsync));
           
                return await _databaseService.GetLocalitiesAsync(cancellationToken);
        }

        public async Task<List<StreetDto>> GetStreetsByLocalityAsync(string localityId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching streets by locality ID: {localityId}. In {method}", localityId, nameof(GetStreetsByLocalityAsync) );
           
            return await _databaseService.GetStreetsByLocalityAsync(localityId, cancellationToken);
        }

        public async Task<LocalityDto?> GetLocalityByIdAsync(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching locality by ID: {id}. In {method}", id, nameof(GetLocalityByIdAsync)); 
             return await _databaseService.GetLocalityByIdAsync(id, cancellationToken);
          
        }

        public async Task UpsertLocalitiesAsync(List<Locality> newData, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Upserting localities. In {method} ", nameof(UpsertLocalitiesAsync));
          
                await _databaseService.UpsertLocalitiesAsync(newData, cancellationToken);

                await _cacheService.EvictCacheAsync("localities", cancellationToken);
            
        }

        public async Task UpsertStreetDataAsync(StreetBatchRequest request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Batch upserting streets. In {method} ", nameof(UpsertStreetDataAsync));
                  await _databaseService.UpsertStreetDataAsync(request, cancellationToken);
                
                if (request.UpdateTimestamp > _lastStreetUpdateTime)
                {
                    _lastStreetUpdateTime = request.UpdateTimestamp;
                    _logger.LogDebug("Evicting streets cache since last update time is now {_lastStreetUpdateTime}.", _lastStreetUpdateTime);
                    await _cacheService.EvictCacheAsync("streets", cancellationToken);
                }
                
        }
    }
}
