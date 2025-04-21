namespace RDS.BackEnd.Accessor.GeolocationInformation.Models;

public class StreetBatchRequest
{
    public required List<Street> Streets { get; set; }
    public DateTime UpdateTimestamp { get; set; }
}