using RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Models;
using System.Text.Json.Nodes;

namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider.Services
{
    public class GeolocationParser(ILogger<GeolocationParser> logger) : IGeolocationParser
    {
        public List<Locality> ParseLocalities(string json)
        {
            try
            {
                var jsonData = JsonNode.Parse(json);
                var records = jsonData?["result"]?["records"]?.AsArray();
                if (records is not null)
                    return records.Select(r => new Locality
                    {
                        id = r?["סמל_ישוב"]?.ToString().Trim() ?? "",
                        localityId = r?["סמל_ישוב"]?.ToString().Trim() ?? "",
                        localityName = r?["שם_ישוב"]?.ToString().Trim() ?? "",
                        createdAt = DateTime.UtcNow
                    }).ToList();
                
                logger.LogWarning("{method}: No 'records' field found in the response.", nameof(ParseLocalities));
                return [];
                
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to parse localities");
                return [];
            }
        }
        public List<Street> ParseStreets(string json)
        {
            try
            {
                var jsonData = JsonNode.Parse(json);
                var records = jsonData?["result"]?["records"]?.AsArray();
                var total = jsonData?["result"]?["total"]?.GetValue<int>() ?? 0;

                if (records is null)
                {
                    logger.LogWarning("{method}: No 'records' field found in the response.", nameof(ParseStreets));
                    return [];
                }
                if (total != records.Count) {
                    logger.LogWarning("{method}: Total records count does not match actual records count.", nameof(ParseStreets)); 
                   return [];
                }

                var streets = records
                    .Select(r => new Street
                    {
                        id = r?["סמל_רחוב"]?.ToString().Trim() ?? "",
                        localityId = r?["סמל_ישוב"]?.ToString().Trim() ?? "",
                        streetId = r?["סמל_רחוב"]?.ToString().Trim() ?? "",
                        streetName = r?["שם_רחוב"]?.ToString().Trim() ?? "",
                        createdAt = DateTime.UtcNow
                    })
                    .Where(s => !string.IsNullOrWhiteSpace(s.id) && !string.IsNullOrWhiteSpace(s.streetId) && !string.IsNullOrWhiteSpace(s.streetName))
                    .ToList();

                // Filter out street ID 9000 if duplicates exist
                var grouped = streets.GroupBy(s => s.localityId);
                var invalidStreetId = InternalConfiguration.Default.First(kv => kv.Key == "InvalidStreetIdToFilter").Value;
                foreach (var g in grouped.Where(g => g.Count() > 1))
                    streets.RemoveAll(s => s.localityId == g.Key && s.streetId == invalidStreetId);

                return streets;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to parse streets");
                return [];
            }
        }
    }
}
