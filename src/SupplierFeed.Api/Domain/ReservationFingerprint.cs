namespace SupplierFeed.Api.Domain;

public sealed class ReservationFingerprint
{
    #region Properties
    public byte[] PayloadHash { get; init; } = Array.Empty<byte>();

    public DateTime UpdatedAtUtc { get; init; }
    #endregion
}
