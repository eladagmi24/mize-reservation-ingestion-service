using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SupplierFeed.Api.Contracts;

namespace SupplierFeed.Tests.Infrastructure;

public static class ApiClientExtensions
{
    #region Fields & Dependencies
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    #endregion

    #region Public Methods
    public static Task<HttpResponseMessage> GetStatsAsync(this HttpClient client, string supplierId)
    {
        return client.GetAsync($"/api/reservations/stats/{supplierId}");
    }

    public static async Task<List<HttpResponseMessage>> IngestNewReservationsSequentiallyAsync(
        this HttpClient client,
        string supplierId,
        int requestCount)
    {
        var responses = new List<HttpResponseMessage>(requestCount);

        for (var requestNumber = 0; requestNumber < requestCount; requestNumber++)
        {
            responses.Add(await client.PostIngestAsync(TestRequests.CreateWithNewReservationId(supplierId)));
        }

        return responses;
    }

    public static Task<HttpResponseMessage> PostIngestAsync(this HttpClient client, ReservationIngestRequest request)
    {
        return client.PostAsJsonAsync("/api/reservations/ingest", request, JsonOptions);
    }

    public static async Task<ReservationIngestResponse> ReadIngestResponseAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ReservationIngestResponse>(JsonOptions);
        return body!;
    }

    public static async Task<SupplierStatsResponse> ReadStatsResponseAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<SupplierStatsResponse>(JsonOptions);
        return body!;
    }
    #endregion
}
