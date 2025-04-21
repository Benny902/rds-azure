using System.Text.Json.Serialization;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Models
{
    public class Locality
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("localityId")]
        public string localityId { get; set; } = string.Empty;

        [JsonPropertyName("localityName")]
        public string localityName { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }
}
