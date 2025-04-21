using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Models;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public interface IGeolocationParser
    {
        List<Locality> ParseLocalities(string json);
        List<Street> ParseStreets(string json);
    }
}
