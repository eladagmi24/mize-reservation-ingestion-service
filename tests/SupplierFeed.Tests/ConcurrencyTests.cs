using System.Net;
using FluentAssertions;
using SupplierFeed.Tests.Infrastructure;

namespace SupplierFeed.Tests;

public sealed class ConcurrencyTests : IDisposable
{
    #region Constants
    private const string SupplierId = "supplier-concurrency";
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(60);
    #endregion

    #region Fields & Dependencies
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;
    #endregion

    #region Constructor
    public ConcurrencyTests()
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
    public async Task Concurrency_150SimultaneousRequestsSingleSupplier_Exactly100IngestedAnd50Throttled()
    {
        const int simultaneousRequestCount = 150;

        var responses = await SendSimultaneouslyAsync(
            Enumerable.Range(0, simultaneousRequestCount).Select(_ => TestRequests.CreateWithNewReservationId(SupplierId)));

        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(100);
        responses.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests).Should().Be(50);
        var stats = await (await _client.GetStatsAsync(SupplierId)).ReadStatsResponseAsync();
        stats.IngestedCount.Should().Be(100);
        stats.ThrottledCount.Should().Be(50);
    }

    [Fact]
    public async Task Concurrency_SimultaneousWritesToSameReservation_SerializedCleanlyWithoutDeadlock()
    {
        const string reservationId = "reservation-contended";
        const int simultaneousWriteCount = 50;
        var baseRequest = TestRequests.Create(SupplierId, reservationId);
        var competingRequests = Enumerable
            .Range(0, simultaneousWriteCount)
            .Select(writeNumber => baseRequest with
            {
                Price = 100.00m + writeNumber,
                UpdatedAtUtc = baseRequest.UpdatedAtUtc.AddMinutes(writeNumber)
            })
            .ToList();

        var responses = await SendSimultaneouslyAsync(competingRequests);

        responses.Should().OnlyContain(response => response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
        responses.Count(response => response.StatusCode == HttpStatusCode.Created).Should().Be(1);
        (await _factory.CountReservationsAsync(SupplierId, reservationId)).Should().Be(1);
        var storedReservation = await _factory.FindReservationAsync(SupplierId, reservationId);
        storedReservation!.UpdatedAtUtc.Should().Be(baseRequest.UpdatedAtUtc.AddMinutes(simultaneousWriteCount - 1));
        storedReservation.Price.Should().Be(100.00m + simultaneousWriteCount - 1);
    }
    #endregion

    #region Private Helpers
    private async Task<HttpResponseMessage[]> SendSimultaneouslyAsync(IEnumerable<SupplierFeed.Api.Contracts.ReservationIngestRequest> requests)
    {
        var startSignal = new TaskCompletionSource();

        var pendingResponses = requests
            .Select(request => Task.Run(async () =>
            {
                await startSignal.Task;
                return await _client.PostIngestAsync(request);
            }))
            .ToList();

        startSignal.SetResult();
        return await Task.WhenAll(pendingResponses).WaitAsync(CompletionTimeout);
    }
    #endregion
}
