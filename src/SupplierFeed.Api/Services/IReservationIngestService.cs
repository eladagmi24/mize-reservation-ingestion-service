using SupplierFeed.Api.Contracts;

namespace SupplierFeed.Api.Services;

public interface IReservationIngestService
{
    #region Operations
    Task<SupplierStatsResponse> GetSupplierStatsAsync(string supplierId, CancellationToken cancellationToken);

    Task<IngestionResult> IngestAsync(ReservationIngestRequest request, CancellationToken cancellationToken);
    #endregion
}
