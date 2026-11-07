using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Contracts;

public sealed record ReservationIngestResponse
{
    #region Properties
    public string Message { get; init; } = string.Empty;

    public IngestionOutcome Status { get; init; }
    #endregion
}
