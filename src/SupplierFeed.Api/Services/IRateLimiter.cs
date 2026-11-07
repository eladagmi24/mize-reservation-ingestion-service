namespace SupplierFeed.Api.Services;

public interface IRateLimiter
{
    #region Operations
    RateLimitDecision TryAcquire(string supplierId);
    #endregion
}
