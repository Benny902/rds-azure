namespace RDS.BackEnd.Manager.Geolocation.Models;

public class StreetBatchRequest
{
    public List<Street> Streets { get; set; } = new();
    public DateTime UpdateTimestamp { get; set; }
}