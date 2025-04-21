using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Models;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public interface IGovernmentGeolocationService
    {
        Task<List<Locality>> FetchGeolocationDataAsync(CancellationToken cancellationToken);
        Task<List<Street>> FetchStreetDataAsync(CancellationToken cancellationToken);
    }
}