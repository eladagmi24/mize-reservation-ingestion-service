namespace SupplierFeed.Api.Contracts;

public sealed record SupplierStatsResponse
{
    #region Properties
    public long IngestedCount { get; init; }

    public string SupplierId { get; init; } = string.Empty;

    public long ThrottledCount { get; init; }
    #endregion
}
