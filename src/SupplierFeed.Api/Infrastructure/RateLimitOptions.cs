namespace SupplierFeed.Api.Infrastructure;

public sealed class RateLimitOptions
{
    #region Constants
    public const string SectionName = "RateLimit";
    #endregion

    #region Properties
    public int RequestLimit { get; init; } = 100;

    public int WindowSeconds { get; init; } = 60;
    #endregion
}
