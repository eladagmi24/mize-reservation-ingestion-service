using FluentAssertions;
using SupplierFeed.Api.Services;

namespace SupplierFeed.Tests;

public sealed class PayloadHasherTests
{
    #region Fields & Dependencies
    private static readonly DateTime CheckIn = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime CheckOut = new(2026, 8, 3, 0, 0, 0, DateTimeKind.Utc);
    #endregion

    #region Public Methods
    [Fact]
    public void ComputeHash_WhenBusinessFieldsAreIdentical_ReturnsSameHash()
    {
        var first = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);
        var second = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);

        first.Should().Equal(second);
        first.Should().HaveCount(32);
    }

    [Fact]
    public void ComputeHash_WhenPriceDiffers_ReturnsDifferentHash()
    {
        var original = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);
        var changed = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.01m);

        changed.Should().NotEqual(original);
    }

    [Fact]
    public void ComputeHash_WhenPriceScaleDiffers_ReturnsSameHash()
    {
        var withTwoDecimals = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);
        var withoutDecimals = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450m);

        withoutDecimals.Should().Equal(withTwoDecimals);
    }

    [Fact]
    public void ComputeHash_WhenRoomIdDiffers_ReturnsDifferentHash()
    {
        var original = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);
        var changed = PayloadHasher.ComputeHash("room-2", CheckIn, CheckOut, 450.00m);

        changed.Should().NotEqual(original);
    }

    [Fact]
    public void ComputeHash_WhenRoomIdHasSurroundingWhitespace_ReturnsSameHash()
    {
        var trimmed = PayloadHasher.ComputeHash("room-1", CheckIn, CheckOut, 450.00m);
        var padded = PayloadHasher.ComputeHash("  room-1  ", CheckIn, CheckOut, 450.00m);

        padded.Should().Equal(trimmed);
    }
    #endregion
}
