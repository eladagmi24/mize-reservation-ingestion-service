using System.ComponentModel.DataAnnotations;

namespace SupplierFeed.Api.Contracts;

public sealed record ReservationIngestRequest : IValidatableObject
{
    #region Constants
    public const int MaximumIdentifierLength = 128;
    #endregion

    #region Properties
    public DateTime CheckIn { get; init; }

    public DateTime CheckOut { get; init; }

    public decimal Price { get; init; }

    public string ReservationId { get; init; } = string.Empty;

    public string RoomId { get; init; } = string.Empty;

    public string SupplierId { get; init; } = string.Empty;

    public DateTime UpdatedAtUtc { get; init; }
    #endregion

    #region Public Methods
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = new List<ValidationResult>();

        validationResults.AddRange(ValidateIdentifier(SupplierId, nameof(SupplierId)));
        validationResults.AddRange(ValidateIdentifier(ReservationId, nameof(ReservationId)));
        validationResults.AddRange(ValidateIdentifier(RoomId, nameof(RoomId)));
        validationResults.AddRange(ValidateUtcDate(CheckIn, nameof(CheckIn)));
        validationResults.AddRange(ValidateUtcDate(CheckOut, nameof(CheckOut)));
        validationResults.AddRange(ValidateUtcDate(UpdatedAtUtc, nameof(UpdatedAtUtc)));

        if (Price <= 0)
        {
            validationResults.Add(new ValidationResult("Price must be greater than zero.", new[] { nameof(Price) }));
        }

        if (IsUtc(CheckIn) && IsUtc(CheckOut) && CheckIn >= CheckOut)
        {
            validationResults.Add(new ValidationResult("CheckIn must be earlier than CheckOut.", new[] { nameof(CheckOut) }));
        }

        return validationResults;
    }
    #endregion

    #region Private Helpers
    private static bool IsUtc(DateTime value)
    {
        return value.Kind == DateTimeKind.Utc;
    }

    private static IEnumerable<ValidationResult> ValidateIdentifier(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            yield return new ValidationResult($"{propertyName} must not be empty.", new[] { propertyName });
        }
        else if (value.Length > MaximumIdentifierLength)
        {
            yield return new ValidationResult(
                $"{propertyName} must not exceed {MaximumIdentifierLength} characters.",
                new[] { propertyName });
        }
    }

    private static IEnumerable<ValidationResult> ValidateUtcDate(DateTime value, string propertyName)
    {
        if (!IsUtc(value))
        {
            yield return new ValidationResult(
                $"{propertyName} must be a valid UTC timestamp (for example 2026-08-01T00:00:00Z).",
                new[] { propertyName });
        }
    }
    #endregion
}
