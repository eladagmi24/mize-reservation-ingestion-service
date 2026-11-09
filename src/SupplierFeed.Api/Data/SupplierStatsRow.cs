namespace SupplierFeed.Api.Data;

internal sealed class SupplierStatsRow
{
    #region Properties
    public long ConflictCount { get; init; }

    public long CreatedCount { get; init; }

    public long DuplicateCount { get; init; }

    public long IngestedCount { get; init; }

    public string? LastRequestUtc { get; init; }

    public long StaleCount { get; init; }

    public string SupplierId { get; init; } = string.Empty;

    public long ThrottledCount { get; init; }

    public long UpdatedCount { get; init; }
    #endregion
}
