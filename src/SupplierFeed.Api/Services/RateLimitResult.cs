namespace SupplierFeed.Api.Services;

public sealed record RateLimitResult
{
    #region Properties
    public bool IsAllowed { get; init; }

    public TimeSpan RetryAfter { get; init; }
    #endregion

    #region Public Methods
    public static RateLimitResult Admitted()
    {
        return new RateLimitResult { IsAllowed = true };
    }

    public static RateLimitResult Rejected(TimeSpan retryAfter)
    {
        return new RateLimitResult { IsAllowed = false, RetryAfter = retryAfter };
    }
    #endregion
}
