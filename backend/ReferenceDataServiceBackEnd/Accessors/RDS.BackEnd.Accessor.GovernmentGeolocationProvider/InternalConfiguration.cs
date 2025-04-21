namespace RDS.BackEnd.Accessor.GovernmentGeolocationProvider
{
    public static class InternalConfiguration
    {
        public static readonly List<KeyValuePair<string, string>> Default = new()
        {
            new KeyValuePair<string, string>("InvalidStreetIdToFilter", "9000"),
            new KeyValuePair<string, string>("RetryCount", "3"),
            new KeyValuePair<string, string>("RetryJitterMillisecondsMax", "100")
        };
    }
}
