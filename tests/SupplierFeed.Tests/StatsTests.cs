using System.Net;
using FluentAssertions;
using SupplierFeed.Tests.Infrastructure;

namespace SupplierFeed.Tests;

public sealed class StatsTests : IDisposable
{
    #region Constants
    private const string SupplierId = "supplier-stats";
    #endregion

    #region Fields & Dependencies
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;
    #endregion

    #region Constructor
    public StatsTests()
    {
        _factory = new ApiFactory();
        _client = _factory.CreateClient();
    }
    #endregion

    #region Setup & Teardown
    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
    #endregion

    #region Integration Tests
    [Fact]
    public async Task Stats_AfterIngestedAndThrottledRequests_ReturnsExactMatchingCounts()
    {
        const int throttledRequestCount = 3;
        const int totalRequestCount = 100 + throttledRequestCount;
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, totalRequestCount);

        var response = await _client.GetStatsAsync(SupplierId);
        var stats = await response.ReadStatsResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stats.SupplierId.Should().Be(SupplierId);
        stats.IngestedCount.Should().Be(100);
        stats.ThrottledCount.Should().Be(throttledRequestCount);
    }

    [Fact]
    public async Task Stats_UnknownSupplierId_Returns200WithZeroCounts()
    {
        const string unknownSupplierId = "supplier-never-seen";

        var response = await _client.GetStatsAsync(unknownSupplierId);
        var stats = await response.ReadStatsResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stats.SupplierId.Should().Be(unknownSupplierId);
        stats.IngestedCount.Should().Be(0);
        stats.ThrottledCount.Should().Be(0);
    }
    #endregion
}
