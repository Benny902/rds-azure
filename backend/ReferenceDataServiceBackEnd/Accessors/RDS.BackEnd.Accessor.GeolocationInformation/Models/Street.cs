using System.Text.Json.Serialization;

namespace RDS.BackEnd.Accessor.GeolocationInformation.Models
{
    public class Street
    {
        [JsonPropertyName("id")]
        public string id { get; set; } = string.Empty;

        [JsonPropertyName("localityId")]
        public string localityId { get; set; } = string.Empty;

        [JsonPropertyName("streetId")]
        public string streetId { get; set; } = string.Empty;

        [JsonPropertyName("streetName")]
        public string streetName { get; set; } = string.Empty;

        [JsonPropertyName("createdAt")]
        public DateTime createdAt { get; set; } = DateTime.UtcNow;
    }

}
