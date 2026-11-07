using System.Globalization;

namespace SupplierFeed.Api.Data;

public static class SqliteTimestamp
{
    #region Constants
    private const string Format = "O";
    #endregion

    #region Public Methods
    public static string ToText(DateTime utcValue)
    {
        return utcValue.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);
    }

    public static DateTime ToUtcDateTime(string text)
    {
        return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
    #endregion
}
