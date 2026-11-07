namespace SupplierFeed.Api.Infrastructure;

public sealed class DatabaseOptions
{
    #region Constants
    public const string SectionName = "Database";
    #endregion

    #region Properties
    public int BusyTimeoutMilliseconds { get; init; } = 5000;

    public string DatabasePath { get; init; } = "supplier-feed.db";
    #endregion
}
