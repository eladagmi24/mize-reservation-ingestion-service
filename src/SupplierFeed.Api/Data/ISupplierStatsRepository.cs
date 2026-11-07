using System.Data.Common;
using SupplierFeed.Api.Domain;

namespace SupplierFeed.Api.Data;

public interface ISupplierStatsRepository
{
    #region Operations
    Task<SupplierStats> GetBySupplierIdAsync(DbConnection connection, string supplierId, CancellationToken cancellationToken);

    Task RecordOutcomeAsync(
        DbConnection connection,
        string supplierId,
        IngestionOutcome outcome,
        DateTime requestedAtUtc,
        CancellationToken cancellationToken);
    #endregion
}
