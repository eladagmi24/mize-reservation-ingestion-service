using System.Data.Common;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data;

public interface IReservationRepository
{
    #region Operations
    Task<Reservation?> GetByCompositeKeyAsync(
        DbConnection connection,
        string supplierId,
        string reservationId,
        CancellationToken cancellationToken);

    Task<ReservationFingerprint?> GetFingerprintByCompositeKeyAsync(
        DbConnection connection,
        string supplierId,
        string reservationId,
        CancellationToken cancellationToken);

    Task InsertAsync(DbConnection connection, Reservation reservation, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(DbConnection connection, Reservation reservation, CancellationToken cancellationToken);
    #endregion
}
