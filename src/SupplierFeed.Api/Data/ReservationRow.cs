namespace SupplierFeed.Api.Data;

internal sealed class ReservationRow
{
    #region Properties
    public string CheckIn { get; init; } = string.Empty;

    public string CheckOut { get; init; } = string.Empty;

    public string FirstSeenUtc { get; init; } = string.Empty;

    public string LastModifiedUtc { get; init; } = string.Empty;

    public byte[] PayloadHash { get; init; } = Array.Empty<byte>();

    public string Price { get; init; } = string.Empty;

    public string ReservationId { get; init; } = string.Empty;

    public string RoomId { get; init; } = string.Empty;

    public string SupplierId { get; init; } = string.Empty;

    public string UpdatedAtUtc { get; init; } = string.Empty;

    public long Version { get; init; }
    #endregion
}
