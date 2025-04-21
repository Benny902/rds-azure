namespace RDS.BackEnd.Manager.Geolocation;

public static class InternalConfiguration
{
    public static readonly List<KeyValuePair<string, string>> Default = new()
    {
        new KeyValuePair<string, string>("StreetUpsertBatchSize", "1000"),
        new KeyValuePair<string, string>("RetryCount", "3"),
        new KeyValuePair<string, string>("RetryBackoffSecondsMultiplier", "2"),
        new KeyValuePair<string, string>("RetryTimeoutMinutes", "5"),
        new KeyValuePair<string, string>("StartupJobTriggerDelaySeconds", "30")
    };
}