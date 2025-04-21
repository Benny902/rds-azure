using RDS.BackEnd.Accessor.GeolocationInformation.Models;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Services;

public interface IDatabaseService
{
    Task InitializeAsync(CancellationToken cancellationToken);
    Task<List<LocalityDto>> GetLocalitiesAsync(CancellationToken cancellationToken);
    Task<List<StreetDto>> GetStreetsByLocalityAsync(string localityId, CancellationToken cancellationToken);
    Task<LocalityDto?> GetLocalityByIdAsync(string id, CancellationToken cancellationToken);
    Task UpsertLocalitiesAsync(List<Locality> newData, CancellationToken cancellationToken);
    Task UpsertStreetDataAsync(StreetBatchRequest request, CancellationToken cancellationToken);
}
