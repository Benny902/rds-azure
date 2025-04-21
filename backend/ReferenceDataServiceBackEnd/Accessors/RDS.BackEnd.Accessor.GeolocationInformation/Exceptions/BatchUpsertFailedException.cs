namespace RDS.BackEnd.Accessor.GeolocationInformation.Exceptions;

public class BulkUpsertFailedException : Exception
{
    public string EntityType { get; }
    public IReadOnlyCollection<string> FailedIds { get; }

    public BulkUpsertFailedException(string entityType, IEnumerable<string> failedIds)
        : base($"Failed to upsert {failedIds.Count()} items of type '{entityType}'.")
    {
        EntityType = entityType;
        FailedIds = failedIds.ToList().AsReadOnly();
    }
}
