namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Configuration;

public class GovernmentApiOptions
{

    public string BaseUrl { get; set; } = string.Empty;
    
    public string LocalitiesEndpoint { get; set; } = string.Empty;
    
    public string StreetsEndpoint { get; set; } = string.Empty;
}