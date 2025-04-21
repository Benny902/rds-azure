using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace RDS.BackEnd.Manager.Geolocation.Utils;

public static class ETagHelper
{
    public static string GenerateETag(object data, bool withTimestamp = true)
    {
        var serialized = JsonSerializer.Serialize(data);
        var hash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(serialized))
        )[..16];

        if (!withTimestamp)
        {
            return $"\"{hash}\"";
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
        return $"\"{hash}-{timestamp}\"";
    }
}