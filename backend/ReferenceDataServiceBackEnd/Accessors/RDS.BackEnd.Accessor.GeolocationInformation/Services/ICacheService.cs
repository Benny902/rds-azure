namespace RDS.BackEnd.Accessor.GeolocationInformation.Services;

public interface ICacheService
{   
    
    // Evict cache by tag, used to invalidate cache for specific categories
    Task EvictCacheAsync(string cacheTag, CancellationToken cancellationToken);

}