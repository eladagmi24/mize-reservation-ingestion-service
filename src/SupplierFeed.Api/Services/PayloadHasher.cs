using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SupplierFeed.Api.Services;

public static class PayloadHasher
{
    #region Constants
    private const string FieldSeparator = "|";
    private const string PriceFormat = "0.00";
    private const string UtcTimestampFormat = "O";
    #endregion

    #region Public Methods
    public static byte[] ComputeHash(string roomId, DateTime checkIn, DateTime checkOut, decimal price)
    {
        var canonicalPayload = BuildCanonicalPayload(roomId, checkIn, checkOut, price);
        return SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
    }
    #endregion

    #region Private Helpers
    private static string BuildCanonicalPayload(string roomId, DateTime checkIn, DateTime checkOut, decimal price)
    {
        return string.Join(
            FieldSeparator,
            roomId.Trim(),
            FormatUtcTimestamp(checkIn),
            FormatUtcTimestamp(checkOut),
            price.ToString(PriceFormat, CultureInfo.InvariantCulture));
    }

    private static string FormatUtcTimestamp(DateTime value)
    {
        return value.ToUniversalTime().ToString(UtcTimestampFormat, CultureInfo.InvariantCulture);
    }
    #endregion
}
