using System.Net;
using FluentAssertions;
using SupplierFeed.Api.Domain;
using SupplierFeed.Tests.Infrastructure;

namespace SupplierFeed.Tests;

public sealed class RateLimitingTests : IDisposable
{
    #region Constants
    private const int WindowRequestLimit = 100;
    private const string SupplierId = "supplier-throttle";
    #endregion

    #region Fields & Dependencies
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;
    #endregion

    #region Constructor
    public RateLimitingTests()
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
    public async Task Throttle_100RequestsSentInSameWindow_AllAcceptedWith200Or201()
    {
        var responses = await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit);

        responses.Should().HaveCount(WindowRequestLimit);
        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task Throttle_101stRequestInSameWindow_Returns429WithRetryAfterHeader()
    {
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit);

        var rejectedResponse = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(SupplierId));
        var rejectedBody = await rejectedResponse.ReadIngestResponseAsync();

        rejectedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejectedResponse.Headers.RetryAfter.Should().NotBeNull();
        rejectedResponse.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(60));
        rejectedBody.Status.Should().Be(IngestionOutcome.Throttled);
    }

    [Fact]
    public async Task Throttle_ClockAdvanced59Seconds_RequestStillRejectedWith429()
    {
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit);
        _factory.Clock.Advance(TimeSpan.FromSeconds(59));

        var rejectedResponse = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(SupplierId));

        rejectedResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejectedResponse.Headers.RetryAfter!.Delta.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Throttle_ClockAdvancedPast60Seconds_OldestSlotEvictedAndRequestAccepted()
    {
        const int firstBatchSize = 50;
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, firstBatchSize);
        _factory.Clock.Advance(TimeSpan.FromSeconds(30));
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit - firstBatchSize);
        var rejectedBeforeEviction = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(SupplierId));
        _factory.Clock.Advance(TimeSpan.FromSeconds(31));

        var acceptedAfterEviction = await _client.IngestNewReservationsSequentiallyAsync(SupplierId, firstBatchSize);
        var rejectedAfterRefill = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(SupplierId));

        rejectedBeforeEviction.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        acceptedAfterEviction.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
        rejectedAfterRefill.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Throttle_SupplierAThrottled_SupplierBRequestsUnimpaired()
    {
        const string throttledSupplierId = "supplier-a";
        const string unaffectedSupplierId = "supplier-b";
        await _client.IngestNewReservationsSequentiallyAsync(throttledSupplierId, WindowRequestLimit);

        var throttledResponse = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(throttledSupplierId));
        var unaffectedResponses = await _client.IngestNewReservationsSequentiallyAsync(unaffectedSupplierId, WindowRequestLimit);

        throttledResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        unaffectedResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task Throttle_ThrottledRequestsDoNotConsumeRollingWindowCapacity()
    {
        const int throttledRequestCount = 20;
        await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit);
        _factory.Clock.Advance(TimeSpan.FromSeconds(30));
        var throttledResponses = await _client.IngestNewReservationsSequentiallyAsync(SupplierId, throttledRequestCount);
        _factory.Clock.Advance(TimeSpan.FromSeconds(30));

        var acceptedResponses = await _client.IngestNewReservationsSequentiallyAsync(SupplierId, WindowRequestLimit);
        var rejectedAfterRefill = await _client.PostIngestAsync(TestRequests.CreateWithNewReservationId(SupplierId));

        throttledResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.TooManyRequests);
        acceptedResponses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created);
        rejectedAfterRefill.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
    #endregion
}
