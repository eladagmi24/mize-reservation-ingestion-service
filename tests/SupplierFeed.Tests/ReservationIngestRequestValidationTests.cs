using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using SupplierFeed.Api.Contracts;

namespace SupplierFeed.Tests;

public sealed class ReservationIngestRequestValidationTests
{
    #region Public Methods
    [Fact]
    public void Validate_WhenCheckInEqualsCheckOut_ReportsCheckOutError()
    {
        var request = CreateValidRequest() with { CheckIn = CreateValidRequest().CheckOut };

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(ReservationIngestRequest.CheckOut));
    }

    [Fact]
    public void Validate_WhenCheckInIsAfterCheckOut_ReportsCheckOutError()
    {
        var request = CreateValidRequest() with { CheckIn = CreateValidRequest().CheckOut.AddDays(1) };

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(ReservationIngestRequest.CheckOut));
    }

    [Fact]
    public void Validate_WhenDateIsMissing_ReportsDateError()
    {
        var request = CreateValidRequest() with { UpdatedAtUtc = default };

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(ReservationIngestRequest.UpdatedAtUtc));
    }

    [Theory]
    [InlineData(nameof(ReservationIngestRequest.CheckIn), DateTimeKind.Local)]
    [InlineData(nameof(ReservationIngestRequest.CheckIn), DateTimeKind.Unspecified)]
    [InlineData(nameof(ReservationIngestRequest.CheckOut), DateTimeKind.Unspecified)]
    [InlineData(nameof(ReservationIngestRequest.UpdatedAtUtc), DateTimeKind.Local)]
    [InlineData(nameof(ReservationIngestRequest.UpdatedAtUtc), DateTimeKind.Unspecified)]
    public void Validate_WhenDateIsNotUtc_ReportsDateError(string propertyName, DateTimeKind dateKind)
    {
        var request = WithDateKind(CreateValidRequest(), propertyName, dateKind);

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(propertyName);
    }

    [Theory]
    [InlineData(nameof(ReservationIngestRequest.ReservationId))]
    [InlineData(nameof(ReservationIngestRequest.RoomId))]
    [InlineData(nameof(ReservationIngestRequest.SupplierId))]
    public void Validate_WhenIdentifierExceedsMaximumLength_ReportsIdentifierError(string propertyName)
    {
        var tooLongValue = new string('x', ReservationIngestRequest.MaximumIdentifierLength + 1);
        var request = WithIdentifier(CreateValidRequest(), propertyName, tooLongValue);

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(propertyName);
    }

    [Theory]
    [InlineData(nameof(ReservationIngestRequest.ReservationId), "")]
    [InlineData(nameof(ReservationIngestRequest.ReservationId), "   ")]
    [InlineData(nameof(ReservationIngestRequest.RoomId), "")]
    [InlineData(nameof(ReservationIngestRequest.RoomId), "   ")]
    [InlineData(nameof(ReservationIngestRequest.SupplierId), "")]
    [InlineData(nameof(ReservationIngestRequest.SupplierId), "   ")]
    public void Validate_WhenIdentifierIsBlank_ReportsIdentifierError(string propertyName, string blankValue)
    {
        var request = WithIdentifier(CreateValidRequest(), propertyName, blankValue);

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(propertyName);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    public void Validate_WhenPriceIsNotPositive_ReportsPriceError(double nonPositivePrice)
    {
        var request = CreateValidRequest() with { Price = (decimal)nonPositivePrice };

        var errors = ValidateRequest(request);

        errors.Should().ContainSingle().Which.MemberNames.Should().Contain(nameof(ReservationIngestRequest.Price));
    }

    [Fact]
    public void Validate_WhenRequestIsValid_ReportsNoErrors()
    {
        var errors = ValidateRequest(CreateValidRequest());

        errors.Should().BeEmpty();
    }
    #endregion

    #region Private Helpers
    private static ReservationIngestRequest CreateValidRequest()
    {
        return new ReservationIngestRequest
        {
            CheckIn = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
            CheckOut = new DateTime(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc),
            Price = 450.00m,
            ReservationId = "reservation-1",
            RoomId = "room-1",
            SupplierId = "supplier-1",
            UpdatedAtUtc = new DateTime(2026, 7, 1, 10, 15, 0, DateTimeKind.Utc)
        };
    }

    private static List<ValidationResult> ValidateRequest(ReservationIngestRequest request)
    {
        var validationContext = new ValidationContext(request);
        return request.Validate(validationContext).ToList();
    }

    private static ReservationIngestRequest WithDateKind(ReservationIngestRequest request, string propertyName, DateTimeKind dateKind)
    {
        return propertyName switch
        {
            nameof(ReservationIngestRequest.CheckIn) => request with { CheckIn = DateTime.SpecifyKind(request.CheckIn, dateKind) },
            nameof(ReservationIngestRequest.CheckOut) => request with { CheckOut = DateTime.SpecifyKind(request.CheckOut, dateKind) },
            nameof(ReservationIngestRequest.UpdatedAtUtc) => request with { UpdatedAtUtc = DateTime.SpecifyKind(request.UpdatedAtUtc, dateKind) },
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null)
        };
    }

    private static ReservationIngestRequest WithIdentifier(ReservationIngestRequest request, string propertyName, string value)
    {
        return propertyName switch
        {
            nameof(ReservationIngestRequest.ReservationId) => request with { ReservationId = value },
            nameof(ReservationIngestRequest.RoomId) => request with { RoomId = value },
            nameof(ReservationIngestRequest.SupplierId) => request with { SupplierId = value },
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null)
        };
    }
    #endregion
}
