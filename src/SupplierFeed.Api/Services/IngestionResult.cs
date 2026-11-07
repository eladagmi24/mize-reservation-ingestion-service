using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Services;

public sealed record IngestionResult
{
    #region Properties
    public string Message { get; init; } = string.Empty;

    public IngestionOutcome Outcome { get; init; }

    public TimeSpan RetryAfter { get; init; }
    #endregion
}
