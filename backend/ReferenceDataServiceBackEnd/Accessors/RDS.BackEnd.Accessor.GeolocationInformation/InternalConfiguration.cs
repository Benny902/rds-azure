namespace RDS.BackEnd.Accessor.GeolocationInformation
{
    public static class InternalConfiguration
    {
        public static readonly List<KeyValuePair<string, string>> Default = new()
        {
            new KeyValuePair<string, string>("RetryCount", "5"),
            new KeyValuePair<string, string>("JitterMaxMilliseconds", "100"),
            new KeyValuePair<string, string>("PartitionKeyPath", "/localityId"),
        };

        public static readonly List<KeyValuePair<string, TimeSpan>> Timeouts = new()
        {
            new("OutputCacheExpiration", TimeSpan.FromMinutes(2))
        };
    }
}
