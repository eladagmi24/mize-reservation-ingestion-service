using System.Data.Common;
using SupplierFeed.Api.Contracts;
using SupplierFeed.Api.Data;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Services;

public sealed class ReservationIngestService : IReservationIngestService
{
    #region Fields & Dependencies
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IRateLimiter _rateLimiter;
    private readonly IReservationRepository _reservationRepository;
    private readonly ISupplierStatsRepository _supplierStatsRepository;
    private readonly TimeProvider _timeProvider;
    #endregion

    #region Constructor
    public ReservationIngestService(
        IDbConnectionFactory connectionFactory,
        IRateLimiter rateLimiter,
        IReservationRepository reservationRepository,
        ISupplierStatsRepository supplierStatsRepository,
        TimeProvider timeProvider)
    {
        _connectionFactory = connectionFactory;
        _rateLimiter = rateLimiter;
        _reservationRepository = reservationRepository;
        _supplierStatsRepository = supplierStatsRepository;
        _timeProvider = timeProvider;
    }
    #endregion

    #region Public Methods
    public async Task<SupplierStatsResponse> GetSupplierStatsAsync(string supplierId, CancellationToken cancellationToken)
    {
        var normalizedSupplierId = supplierId.Trim();

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        var stats = await _supplierStatsRepository.GetBySupplierIdAsync(connection, normalizedSupplierId, cancellationToken);

        return new SupplierStatsResponse
        {
            IngestedCount = stats.IngestedCount,
            SupplierId = normalizedSupplierId,
            ThrottledCount = stats.ThrottledCount
        };
    }

    public async Task<IngestionResult> IngestAsync(ReservationIngestRequest request, CancellationToken cancellationToken)
    {
        var incomingReservation = BuildIncomingReservation(request);

        var rateLimitDecision = await _rateLimiter.TryAcquireAsync(incomingReservation.SupplierId, cancellationToken);
        if (!rateLimitDecision.IsAllowed)
        {
            await RecordThrottledRequestAsync(incomingReservation, cancellationToken);
            return CreateResult(IngestionOutcome.Throttled, rateLimitDecision.RetryAfter);
        }

        var outcome = await PersistAsync(incomingReservation, cancellationToken);
        return CreateResult(outcome, TimeSpan.Zero);
    }
    #endregion

    #region Private Helpers
    private async Task<IngestionOutcome> ApplyIncomingReservationAsync(
        DbConnection connection,
        Reservation incomingReservation,
        ReservationFingerprint? storedFingerprint,
        CancellationToken cancellationToken)
    {
        if (storedFingerprint is null)
        {
            await _reservationRepository.InsertAsync(connection, incomingReservation, cancellationToken);
            return IngestionOutcome.Created;
        }

        if (incomingReservation.PayloadHash.AsSpan().SequenceEqual(storedFingerprint.PayloadHash))
        {
            return IngestionOutcome.Duplicate;
        }

        if (incomingReservation.UpdatedAtUtc < storedFingerprint.UpdatedAtUtc)
        {
            return IngestionOutcome.StaleIgnored;
        }

        if (incomingReservation.UpdatedAtUtc == storedFingerprint.UpdatedAtUtc)
        {
            return IngestionOutcome.Conflict;
        }

        var wasUpdated = await _reservationRepository.UpdateAsync(connection, incomingReservation, cancellationToken);
        return wasUpdated ? IngestionOutcome.Updated : IngestionOutcome.StaleIgnored;
    }

    private Reservation BuildIncomingReservation(ReservationIngestRequest request)
    {
        var receivedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var roomId = request.RoomId.Trim();

        return new Reservation
        {
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            FirstSeenUtc = receivedAtUtc,
            LastModifiedUtc = receivedAtUtc,
            PayloadHash = PayloadHasher.ComputeHash(roomId, request.CheckIn, request.CheckOut, request.Price),
            Price = request.Price,
            ReservationId = request.ReservationId.Trim(),
            RoomId = roomId,
            SupplierId = request.SupplierId.Trim(),
            UpdatedAtUtc = request.UpdatedAtUtc,
            Version = 1
        };
    }

    private static IngestionResult CreateResult(IngestionOutcome outcome, TimeSpan retryAfter)
    {
        return new IngestionResult
        {
            Message = DescribeOutcome(outcome),
            Outcome = outcome,
            RetryAfter = retryAfter
        };
    }

    private static string DescribeOutcome(IngestionOutcome outcome)
    {
        return outcome switch
        {
            IngestionOutcome.Conflict => "Incoming update has the same updatedAtUtc as the current record but different details",
            IngestionOutcome.Created => "Reservation created",
            IngestionOutcome.Duplicate => "Reservation details are unchanged; no update applied",
            IngestionOutcome.StaleIgnored => "Incoming update is older than the current record",
            IngestionOutcome.Throttled => "Supplier rate limit exceeded; retry after the indicated delay",
            IngestionOutcome.Updated => "Reservation updated with newer details",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null)
        };
    }

    private async Task<IngestionOutcome> PersistAsync(Reservation incomingReservation, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await ImmediateTransaction.BeginAsync(connection, cancellationToken);

        var storedFingerprint = await _reservationRepository.GetFingerprintByCompositeKeyAsync(
            connection,
            incomingReservation.SupplierId,
            incomingReservation.ReservationId,
            cancellationToken);

        var outcome = await ApplyIncomingReservationAsync(connection, incomingReservation, storedFingerprint, cancellationToken);

        await _supplierStatsRepository.RecordOutcomeAsync(
            connection,
            incomingReservation.SupplierId,
            outcome,
            incomingReservation.LastModifiedUtc,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return outcome;
    }

    private async Task RecordThrottledRequestAsync(Reservation incomingReservation, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await _supplierStatsRepository.RecordOutcomeAsync(
            connection,
            incomingReservation.SupplierId,
            IngestionOutcome.Throttled,
            incomingReservation.LastModifiedUtc,
            cancellationToken);
    }
    #endregion
}
