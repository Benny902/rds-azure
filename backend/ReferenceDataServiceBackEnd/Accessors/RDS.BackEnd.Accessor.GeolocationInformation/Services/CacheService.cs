namespace RDS.BackEnd.Accessor.GeolocationInformation.Services;

using Microsoft.AspNetCore.OutputCaching;

public class CacheService(IOutputCacheStore outputCacheStore) :ICacheService
{
    public async Task EvictCacheAsync(string cacheTag, CancellationToken cancellationToken)
    {
        // Evicts the cache using the given cache tag
        await outputCacheStore.EvictByTagAsync(cacheTag, cancellationToken);
    }
    
}
