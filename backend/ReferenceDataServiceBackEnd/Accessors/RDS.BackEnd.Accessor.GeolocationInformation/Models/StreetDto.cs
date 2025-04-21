using System.Text.Json.Serialization;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Models
{
    public class StreetDto
    {
        [JsonPropertyName("localityId")]
        public string LocalityId { get; set; } = string.Empty;

        [JsonPropertyName("streetId")]
        public string StreetId { get; set; } = string.Empty;

        [JsonPropertyName("streetName")]
        public string StreetName { get; set; } = string.Empty;
    }
}
