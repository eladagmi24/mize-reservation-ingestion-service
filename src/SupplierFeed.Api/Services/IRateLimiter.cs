namespace SupplierFeed.Api.Services;

public interface IRateLimiter
{
    #region Operations
    Task<RateLimitResult> TryAcquireAsync(string supplierId, CancellationToken cancellationToken);
    #endregion
}
