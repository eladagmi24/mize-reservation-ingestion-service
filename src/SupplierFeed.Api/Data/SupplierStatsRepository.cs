using System.Data.Common;
using Dapper;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data;

public sealed class SupplierStatsRepository : ISupplierStatsRepository
{
    #region Constants
    private const string SelectBySupplierIdQuery = """
        SELECT supplier_id AS SupplierId,
               ingested_count AS IngestedCount,
               throttled_count AS ThrottledCount,
               created_count AS CreatedCount,
               updated_count AS UpdatedCount,
               duplicate_count AS DuplicateCount,
               stale_count AS StaleCount,
               conflict_count AS ConflictCount,
               last_request_utc AS LastRequestUtc
        FROM supplier_stats
        WHERE supplier_id = @SupplierId;
        """;

    private const string UpsertOutcomeCommand = """
        INSERT INTO supplier_stats (
            supplier_id, ingested_count, throttled_count, created_count, updated_count,
            duplicate_count, stale_count, conflict_count, last_request_utc)
        VALUES (
            @SupplierId, @IngestedIncrement, @ThrottledIncrement, @CreatedIncrement, @UpdatedIncrement,
            @DuplicateIncrement, @StaleIncrement, @ConflictIncrement, @RequestedAtUtc)
        ON CONFLICT (supplier_id) DO UPDATE SET
            ingested_count = ingested_count + excluded.ingested_count,
            throttled_count = throttled_count + excluded.throttled_count,
            created_count = created_count + excluded.created_count,
            updated_count = updated_count + excluded.updated_count,
            duplicate_count = duplicate_count + excluded.duplicate_count,
            stale_count = stale_count + excluded.stale_count,
            conflict_count = conflict_count + excluded.conflict_count,
            last_request_utc = excluded.last_request_utc;
        """;
    #endregion

    #region Public Methods
    public async Task<SupplierStats> GetBySupplierIdAsync(
        DbConnection connection,
        string supplierId,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SelectBySupplierIdQuery,
            new { SupplierId = supplierId },
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<SupplierStatsRow>(command);

        return row is null
            ? new SupplierStats { SupplierId = supplierId }
            : new SupplierStats
            {
                ConflictCount = row.ConflictCount,
                CreatedCount = row.CreatedCount,
                DuplicateCount = row.DuplicateCount,
                IngestedCount = row.IngestedCount,
                LastRequestUtc = row.LastRequestUtc is null ? null : SqliteTimestamp.ToUtcDateTime(row.LastRequestUtc),
                StaleCount = row.StaleCount,
                SupplierId = row.SupplierId,
                ThrottledCount = row.ThrottledCount,
                UpdatedCount = row.UpdatedCount
            };
    }

    public async Task RecordOutcomeAsync(
        DbConnection connection,
        string supplierId,
        IngestionOutcome outcome,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken)
    {
        var parameters = new
        {
            ConflictIncrement = CountIf(outcome == IngestionOutcome.Conflict),
            CreatedIncrement = CountIf(outcome == IngestionOutcome.Created),
            DuplicateIncrement = CountIf(outcome == IngestionOutcome.Duplicate),
            IngestedIncrement = CountIf(outcome != IngestionOutcome.Throttled),
            RequestedAtUtc = SqliteTimestamp.ToText(requestedAtUtc),
            StaleIncrement = CountIf(outcome == IngestionOutcome.StaleIgnored),
            SupplierId = supplierId,
            ThrottledIncrement = CountIf(outcome == IngestionOutcome.Throttled),
            UpdatedIncrement = CountIf(outcome == IngestionOutcome.Updated)
        };

        await connection.ExecuteAsync(new CommandDefinition(UpsertOutcomeCommand, parameters, cancellationToken: cancellationToken));
    }
    #endregion

    #region Private Helpers
    private static int CountIf(bool condition)
    {
        return condition ? 1 : 0;
    }
    #endregion

    #region Nested Types
    private sealed class SupplierStatsRow
    {
        #region Properties
        public long ConflictCount { get; init; }

        public long CreatedCount { get; init; }

        public long DuplicateCount { get; init; }

        public long IngestedCount { get; init; }

        public string? LastRequestUtc { get; init; }

        public long StaleCount { get; init; }

        public string SupplierId { get; init; } = string.Empty;

        public long ThrottledCount { get; init; }

        public long UpdatedCount { get; init; }
        #endregion
    }
    #endregion
}
