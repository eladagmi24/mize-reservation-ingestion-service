using System.Data.Common;
using System.Globalization;
using Dapper;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data;

public sealed class ReservationRepository : IReservationRepository
{
    #region Constants
    private const string InsertCommand = """
        INSERT INTO reservations (
            supplier_id, reservation_id, room_id, check_in_utc, check_out_utc, price,
            payload_hash, updated_at_utc, first_seen_utc, last_modified_utc, version)
        VALUES (
            @SupplierId, @ReservationId, @RoomId, @CheckIn, @CheckOut, @Price,
            @PayloadHash, @UpdatedAtUtc, @FirstSeenUtc, @LastModifiedUtc, @Version);
        """;

    private const string SelectByCompositeKeyQuery = """
        SELECT supplier_id AS SupplierId,
               reservation_id AS ReservationId,
               room_id AS RoomId,
               check_in_utc AS CheckIn,
               check_out_utc AS CheckOut,
               price AS Price,
               payload_hash AS PayloadHash,
               updated_at_utc AS UpdatedAtUtc,
               first_seen_utc AS FirstSeenUtc,
               last_modified_utc AS LastModifiedUtc,
               version AS Version
        FROM reservations
        WHERE supplier_id = @SupplierId AND reservation_id = @ReservationId;
        """;

    private const string SelectFingerprintQuery = """
        SELECT payload_hash AS PayloadHash,
               updated_at_utc AS UpdatedAtUtc
        FROM reservations
        WHERE supplier_id = @SupplierId AND reservation_id = @ReservationId;
        """;

    private const string UpdateCommand = """
        UPDATE reservations
        SET room_id = @RoomId,
            check_in_utc = @CheckIn,
            check_out_utc = @CheckOut,
            price = @Price,
            payload_hash = @PayloadHash,
            updated_at_utc = @UpdatedAtUtc,
            last_modified_utc = @LastModifiedUtc,
            version = version + 1
        WHERE supplier_id = @SupplierId
          AND reservation_id = @ReservationId
          AND updated_at_utc < @UpdatedAtUtc;
        """;
    #endregion

    #region Public Methods
    public async Task<Reservation?> GetByCompositeKeyAsync(
        DbConnection connection,
        string supplierId,
        string reservationId,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SelectByCompositeKeyQuery,
            new { SupplierId = supplierId, ReservationId = reservationId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ReservationRow>(command);
        return row is null ? null : ToReservation(row);
    }

    public async Task<ReservationFingerprint?> GetFingerprintByCompositeKeyAsync(
        DbConnection connection,
        string supplierId,
        string reservationId,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SelectFingerprintQuery,
            new { SupplierId = supplierId, ReservationId = reservationId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<FingerprintRow>(command);
        return row is null
            ? null
            : new ReservationFingerprint
            {
                PayloadHash = row.PayloadHash,
                UpdatedAtUtc = SqliteTimestamp.ToUtcDateTime(row.UpdatedAtUtc)
            };
    }

    public async Task InsertAsync(DbConnection connection, Reservation reservation, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            InsertCommand,
            ToParameters(reservation),
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    public async Task<bool> UpdateAsync(DbConnection connection, Reservation reservation, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            UpdateCommand,
            ToParameters(reservation),
            cancellationToken: cancellationToken);

        var affectedRows = await connection.ExecuteAsync(command);
        return affectedRows == 1;
    }
    #endregion

    #region Private Helpers
    private static object ToParameters(Reservation reservation)
    {
        return new
        {
            CheckIn = SqliteTimestamp.ToText(reservation.CheckIn),
            CheckOut = SqliteTimestamp.ToText(reservation.CheckOut),
            FirstSeenUtc = SqliteTimestamp.ToText(reservation.FirstSeenUtc),
            LastModifiedUtc = SqliteTimestamp.ToText(reservation.LastModifiedUtc),
            reservation.PayloadHash,
            Price = reservation.Price.ToString(CultureInfo.InvariantCulture),
            reservation.ReservationId,
            reservation.RoomId,
            reservation.SupplierId,
            UpdatedAtUtc = SqliteTimestamp.ToText(reservation.UpdatedAtUtc),
            reservation.Version
        };
    }

    private static Reservation ToReservation(ReservationRow row)
    {
        return new Reservation
        {
            CheckIn = SqliteTimestamp.ToUtcDateTime(row.CheckIn),
            CheckOut = SqliteTimestamp.ToUtcDateTime(row.CheckOut),
            FirstSeenUtc = SqliteTimestamp.ToUtcDateTime(row.FirstSeenUtc),
            LastModifiedUtc = SqliteTimestamp.ToUtcDateTime(row.LastModifiedUtc),
            PayloadHash = row.PayloadHash,
            Price = decimal.Parse(row.Price, CultureInfo.InvariantCulture),
            ReservationId = row.ReservationId,
            RoomId = row.RoomId,
            SupplierId = row.SupplierId,
            UpdatedAtUtc = SqliteTimestamp.ToUtcDateTime(row.UpdatedAtUtc),
            Version = row.Version
        };
    }
    #endregion
}
