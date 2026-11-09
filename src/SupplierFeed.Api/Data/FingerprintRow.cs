namespace SupplierFeed.Api.Data;

internal sealed class FingerprintRow
{
    #region Properties
    public byte[] PayloadHash { get; init; } = Array.Empty<byte>();

    public string UpdatedAtUtc { get; init; } = string.Empty;
    #endregion
}
