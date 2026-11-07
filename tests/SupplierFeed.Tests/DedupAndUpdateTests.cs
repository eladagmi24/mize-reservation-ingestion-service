using System.Net;
using FluentAssertions;
using SupplierFeed.Api.Domain;
using SupplierFeed.Tests.Infrastructure;

namespace SupplierFeed.Tests;

public sealed class DedupAndUpdateTests : IDisposable
{
    #region Constants
    private const string ReservationId = "reservation-1";
    private const string SupplierId = "supplier-1";
    #endregion

    #region Fields & Dependencies
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;
    #endregion

    #region Constructor
    public DedupAndUpdateTests()
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
    public async Task Ingest_ConflictingDataWithSameTimestamp_Returns409Conflict()
    {
        var originalRequest = TestRequests.Create(SupplierId, ReservationId);
        await _client.PostIngestAsync(originalRequest);
        var conflictingRequest = originalRequest with { Price = 999.00m };

        var response = await _client.PostIngestAsync(conflictingRequest);
        var body = await response.ReadIngestResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        body.Status.Should().Be(IngestionOutcome.Conflict);
        var storedReservation = await _factory.FindReservationAsync(SupplierId, ReservationId);
        storedReservation!.Price.Should().Be(originalRequest.Price);
    }

    [Fact]
    public async Task Ingest_ExistingReservationWithNewerTimestamp_Returns200UpdatedAndPersistsNewValues()
    {
        var originalRequest = TestRequests.Create(SupplierId, ReservationId);
        await _client.PostIngestAsync(originalRequest);
        var newerRequest = originalRequest with
        {
            Price = 500.00m,
            RoomId = "room-2",
            UpdatedAtUtc = originalRequest.UpdatedAtUtc.AddDays(1)
        };

        var response = await _client.PostIngestAsync(newerRequest);
        var body = await response.ReadIngestResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Status.Should().Be(IngestionOutcome.Updated);
        var storedReservation = await _factory.FindReservationAsync(SupplierId, ReservationId);
        storedReservation!.Price.Should().Be(newerRequest.Price);
        storedReservation.RoomId.Should().Be(newerRequest.RoomId);
        storedReservation.UpdatedAtUtc.Should().Be(newerRequest.UpdatedAtUtc);
        storedReservation.Version.Should().Be(2);
    }

    [Fact]
    public async Task Ingest_ExistingReservationWithOlderTimestamp_Returns200StaleIgnoredAndPreservesExistingData()
    {
        var originalRequest = TestRequests.Create(SupplierId, ReservationId);
        await _client.PostIngestAsync(originalRequest);
        var storedBeforeStaleUpdate = await _factory.FindReservationAsync(SupplierId, ReservationId);
        var staleRequest = originalRequest with
        {
            Price = 300.00m,
            UpdatedAtUtc = originalRequest.UpdatedAtUtc.AddDays(-30)
        };

        var response = await _client.PostIngestAsync(staleRequest);
        var body = await response.ReadIngestResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Status.Should().Be(IngestionOutcome.StaleIgnored);
        body.Message.Should().Contain("older");
        var storedAfterStaleUpdate = await _factory.FindReservationAsync(SupplierId, ReservationId);
        storedAfterStaleUpdate.Should().BeEquivalentTo(storedBeforeStaleUpdate);
    }

    [Fact]
    public async Task Ingest_FirstTimePayload_Returns201CreatedAndPersistsRow()
    {
        var request = TestRequests.Create(SupplierId, ReservationId);

        var response = await _client.PostIngestAsync(request);
        var body = await response.ReadIngestResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        body.Status.Should().Be(IngestionOutcome.Created);
        var storedReservation = await _factory.FindReservationAsync(SupplierId, ReservationId);
        storedReservation.Should().NotBeNull();
        storedReservation!.CheckIn.Should().Be(request.CheckIn);
        storedReservation.CheckOut.Should().Be(request.CheckOut);
        storedReservation.Price.Should().Be(request.Price);
        storedReservation.RoomId.Should().Be(request.RoomId);
        storedReservation.UpdatedAtUtc.Should().Be(request.UpdatedAtUtc);
        storedReservation.Version.Should().Be(1);
    }

    [Fact]
    public async Task Ingest_IdenticalPayloadResent_Returns200DuplicateWithoutModifyingDb()
    {
        var request = TestRequests.Create(SupplierId, ReservationId);
        await _client.PostIngestAsync(request);
        var storedBeforeResend = await _factory.FindReservationAsync(SupplierId, ReservationId);
        _factory.Clock.Advance(TimeSpan.FromMinutes(10));

        var response = await _client.PostIngestAsync(request);
        var body = await response.ReadIngestResponseAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Status.Should().Be(IngestionOutcome.Duplicate);
        var storedAfterResend = await _factory.FindReservationAsync(SupplierId, ReservationId);
        storedAfterResend.Should().BeEquivalentTo(storedBeforeResend);
        (await _factory.CountReservationsAsync(SupplierId, ReservationId)).Should().Be(1);
    }

    [Fact]
    public async Task Ingest_SameReservationIdFromDifferentSuppliers_TreatedAsIndependentEntities()
    {
        const string firstSupplierId = "supplier-a";
        const string secondSupplierId = "supplier-b";
        var firstSupplierRequest = TestRequests.Create(firstSupplierId, ReservationId);
        var secondSupplierRequest = TestRequests.Create(secondSupplierId, ReservationId) with { Price = 700.00m };

        var firstResponse = await _client.PostIngestAsync(firstSupplierRequest);
        var secondResponse = await _client.PostIngestAsync(secondSupplierRequest);
        var firstSupplierUpdate = firstSupplierRequest with
        {
            Price = 800.00m,
            UpdatedAtUtc = firstSupplierRequest.UpdatedAtUtc.AddDays(1)
        };
        var updateResponse = await _client.PostIngestAsync(firstSupplierUpdate);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstSupplierStored = await _factory.FindReservationAsync(firstSupplierId, ReservationId);
        var secondSupplierStored = await _factory.FindReservationAsync(secondSupplierId, ReservationId);
        firstSupplierStored!.Price.Should().Be(800.00m);
        firstSupplierStored.Version.Should().Be(2);
        secondSupplierStored!.Price.Should().Be(700.00m);
        secondSupplierStored.Version.Should().Be(1);
    }
    #endregion
}
