
namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public class GovernmentApiClient(HttpClient httpClient, ILogger<GovernmentApiClient> logger) : IGovernmentApiClient
    {
        public async Task<string> GetRawDataAsync(string relativeUrl, CancellationToken cancellationToken)
        {
            try
            {
                var response = await httpClient.GetAsync(relativeUrl, cancellationToken);

                if (response.IsSuccessStatusCode) return await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning("Failed to get data from {relativeUrl}. Status code: {response.StatusCode}",relativeUrl, response.StatusCode);
                return string.Empty;

            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Request to {relativeUrl} was cancelled.", relativeUrl);
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get data from {relativeUrl}",relativeUrl);
                return string.Empty;
            }
        }
    }
}
