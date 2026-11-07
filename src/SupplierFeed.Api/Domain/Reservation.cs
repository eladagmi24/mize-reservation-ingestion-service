namespace SupplierFeed.Api.Domain;

public sealed class Reservation
{
    #region Properties
    public DateTime CheckIn { get; init; }

    public DateTime CheckOut { get; init; }

    public DateTime FirstSeenUtc { get; init; }

    public DateTime LastModifiedUtc { get; init; }

    public byte[] PayloadHash { get; init; } = Array.Empty<byte>();

    public decimal Price { get; init; }

    public string ReservationId { get; init; } = string.Empty;

    public string RoomId { get; init; } = string.Empty;

    public string SupplierId { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; init; }

    public long Version { get; init; }
    #endregion
}
