using System.Text.Json.Serialization;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Models
{
    public class LocalityDto
    {
        [JsonPropertyName("localityId")]
        public string LocalityId { get; set; } = string.Empty;

        [JsonPropertyName("localityName")]
        public string LocalityName { get; set; } = string.Empty;
    }
}
