using SupplierFeed.Api.Contracts;

namespace SupplierFeed.Tests.Infrastructure;

public static class TestRequests
{
    #region Fields & Dependencies
    public static readonly DateTime DefaultCheckIn = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultCheckOut = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime DefaultUpdatedAtUtc = new(2026, 7, 1, 10, 15, 0, DateTimeKind.Utc);
    #endregion

    #region Public Methods
    public static ReservationIngestRequest Create(string supplierId, string reservationId)
    {
        return new ReservationIngestRequest
        {
            CheckIn = DefaultCheckIn,
            CheckOut = DefaultCheckOut,
            Price = 450.00m,
            ReservationId = reservationId,
            RoomId = "room-1",
            SupplierId = supplierId,
            UpdatedAtUtc = DefaultUpdatedAtUtc
        };
    }

    public static ReservationIngestRequest CreateWithNewReservationId(string supplierId)
    {
        return Create(supplierId, Guid.NewGuid().ToString("N"));
    }
    #endregion
}
