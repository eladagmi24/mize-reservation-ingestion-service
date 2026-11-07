namespace SupplierFeed.Api.Services;

public sealed record RateLimitDecision
{
    #region Properties
    public bool IsAllowed { get; init; }

    public TimeSpan RetryAfter { get; init; }
    #endregion

    #region Public Methods
    public static RateLimitDecision Admitted()
    {
        return new RateLimitDecision { IsAllowed = true };
    }

    public static RateLimitDecision Rejected(TimeSpan retryAfter)
    {
        return new RateLimitDecision { IsAllowed = false, RetryAfter = retryAfter };
    }
    #endregion
}
