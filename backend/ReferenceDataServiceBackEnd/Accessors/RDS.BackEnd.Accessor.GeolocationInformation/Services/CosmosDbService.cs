using System.Globalization;
using System.Collections.Concurrent;
using System.Net;
using Microsoft.Azure.Cosmos;
using Polly;
using Polly.Retry;
using RDS.BackEnd.Accessor.GeolocationInformation.Exceptions;
using RDS.BackEnd.Accessor.GeolocationInformation.Models;
using System.Diagnostics;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Services;

public class CosmosDbService : IDatabaseService
{
    private readonly CosmosClient _cosmosClient;
    private Container? _localitiesContainer;
    private Container? _streetsContainer;
    private readonly string _databaseId;
    private readonly string _localitiesContainerId;
    private readonly string _streetsContainerId;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ILogger<CosmosDbService> _logger;

    public CosmosDbService(CosmosClient cosmosClient, IConfiguration config, ILogger<CosmosDbService> logger)
    {
        _cosmosClient = cosmosClient;
        _logger = logger;
        _databaseId = config["CosmosDb:DatabaseId"] ?? throw new ArgumentNullException("CosmosDb:DatabaseId");
        _localitiesContainerId = config["CosmosDb:ContainerId"] ?? throw new ArgumentNullException("CosmosDb:ContainerId");
        _streetsContainerId = config["CosmosDb:StreetsContainerId"] ?? throw new ArgumentNullException("CosmosDb:StreetsContainerId");
        int retryCount = int.Parse(
            InternalConfiguration.Default.First(kv => kv.Key == "RetryCount").Value,
            CultureInfo.InvariantCulture
        );
        int jitterMax = int.Parse(
            InternalConfiguration.Default.First(kv => kv.Key == "JitterMaxMilliseconds").Value,
            CultureInfo.InvariantCulture
        );

        _retryPolicy = Policy
            .Handle<CosmosException>(ex => ex.StatusCode is HttpStatusCode.TooManyRequests
                or HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Min(8, Math.Pow(2, retryAttempt))) +
                                TimeSpan.FromMilliseconds(new Random().Next(0, jitterMax)),
                (exception, timeSpan, retryAttempt, _) =>
                {
                    _logger.LogWarning(
                        $"Retry {retryAttempt} after {timeSpan.TotalSeconds} seconds due to {exception.Message}");
                });
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Cosmos DB containers. In {method} ", nameof(InitializeAsync));
        try
        {
            await _retryPolicy.ExecuteAsync(async () =>
            {
                // Create database
                var databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseId,
                    cancellationToken:
                    cancellationToken);
                var database = databaseResponse.Database;

                // Create containers
                _localitiesContainer = await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(_localitiesContainerId, InternalConfiguration.Default.First(kv => kv.Key == "PartitionKeyPath").Value),
                    cancellationToken: cancellationToken);

                _streetsContainer = await database.CreateContainerIfNotExistsAsync(
                    new ContainerProperties(_streetsContainerId, InternalConfiguration.Default.First(kv => kv.Key == "PartitionKeyPath").Value),
                    cancellationToken: cancellationToken);
            });

            _logger.LogInformation("Cosmos DB containers initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Cosmos DB containers. In {method} ", nameof(InitializeAsync));
            throw;
        }
    }


    public async Task<List<LocalityDto>> GetLocalitiesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching localities. In {method} ", nameof(GetLocalitiesAsync));

        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = await _retryPolicy.ExecuteAsync(() =>
            Task.FromResult(_localitiesContainer?.GetItemQueryIterator<LocalityDto>(query)));

        var results = new List<LocalityDto>();
        while (iterator is { HasMoreResults: true })
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }
  
        return results.Count != 0 ? results : [];
    }

    public async Task<List<StreetDto>> GetStreetsByLocalityAsync(string localityId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching streets by locality ID: {localityId}. In {method} ", localityId,
            nameof(GetStreetsByLocalityAsync));

        var query = new QueryDefinition("SELECT * FROM c WHERE c.localityId = @id")
            .WithParameter("@id", localityId);
        var iterator = await _retryPolicy.ExecuteAsync(() =>
            Task.FromResult(_streetsContainer?.GetItemQueryIterator<StreetDto>(query)));

        var results = new List<StreetDto>();
        while (iterator is { HasMoreResults: true })
        {
            var response = await iterator.ReadNextAsync(cancellationToken);
            results.AddRange(response);
        }

        return results.Count != 0 ? results : [];
    }

    public async Task<LocalityDto?> GetLocalityByIdAsync(string id, CancellationToken cancellationToken)
    {

        _logger.LogInformation("Fetching locality by ID: {id}. In {method} ", id, nameof(GetLocalityByIdAsync));
        var query = new QueryDefinition("SELECT * FROM c WHERE c.localityId = @id").WithParameter("@id", id);
        var iterator = await _retryPolicy.ExecuteAsync(() =>
            Task.FromResult(_localitiesContainer?.GetItemQueryIterator<LocalityDto>(query)));

        if (iterator is not { HasMoreResults: true }) return null;
        var response = await iterator.ReadNextAsync(cancellationToken);
        return response.FirstOrDefault();
    }


    
    public async Task UpsertLocalitiesAsync(List<Locality> newData, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting BULK upsert of localities");

        var totalRu = 0L;
        var failedLocalities = new ConcurrentBag<string>();

        var records = newData
            .Where(l => !string.IsNullOrEmpty(l.localityId))
            .ToList();

        var tasks = records.Select(async locality =>
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _localitiesContainer!.UpsertItemAsync(
                        item: locality,
                        partitionKey: new PartitionKey(locality.localityId),
                        cancellationToken: cancellationToken));

                Interlocked.Add(ref totalRu, (long)(response.RequestCharge * 1000)); // שמור במיליר״יו
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert locality {LocalityId}", locality.localityId);
                failedLocalities.Add(locality.localityId);
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("Finished bulk upserting {Count} localities. Total RU: {TotalRu}",
            records.Count, totalRu / 1000);

        if (!failedLocalities.IsEmpty)
        {
            _logger.LogWarning("Bulk UpsertLocalitiesAsync completed with failures. Failed locality IDs: {FailedLocalities}",
                failedLocalities);
            throw new BulkUpsertFailedException ("Localities",failedLocalities );
        }
    }

    public async Task UpsertStreetDataAsync(StreetBatchRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting BULK upsert of streets");

        var totalRu = 0L;
        var failedStreets = new ConcurrentBag<string>();

        var validStreets = request.Streets
            .Where(s => !string.IsNullOrEmpty(s.localityId))
            .ToList();

        var tasks = validStreets.Select(async street =>
        {
            try
            {
                var response = await _retryPolicy.ExecuteAsync(() =>
                    _streetsContainer!.UpsertItemAsync(
                        item: street,
                        partitionKey: new PartitionKey(street.localityId),
                        cancellationToken: cancellationToken));

                Interlocked.Add(ref totalRu, (long)(response.RequestCharge * 1000)); // שמור במיליר״יו, כדי להימנע ממעבר בין threads
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upsert street {StreetId}", street.streetId);
                failedStreets.Add(street.streetId);
            }
        });

        await Task.WhenAll(tasks);

        stopwatch.Stop();
        _logger.LogInformation("Finished bulk upserting {Count} streets in {ElapsedSeconds} seconds. Total RU: {TotalRu}",
            validStreets.Count, stopwatch.Elapsed.TotalSeconds, totalRu / 1000);

        if (!failedStreets.IsEmpty)
        {
            _logger.LogWarning("Bulk UpsertStreetDataAsync completed with failures. Failed street IDs: {FailedStreets}",
                failedStreets);
            throw new BulkUpsertFailedException("Street",failedStreets);
        }
        
    }

}