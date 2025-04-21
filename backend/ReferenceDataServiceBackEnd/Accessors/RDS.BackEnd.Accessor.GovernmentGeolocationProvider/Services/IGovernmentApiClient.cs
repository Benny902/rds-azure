namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public interface IGovernmentApiClient
    {
        Task<string> GetRawDataAsync(string relativeUrl, CancellationToken cancellationToken);

    }
}
