namespace SupplierFeed.Api.Domain;

public enum IngestionOutcome
{
    #region Outcomes
    Conflict,
    Created,
    Duplicate,
    StaleIgnored,
    Throttled,
    Updated
    #endregion
}
