using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SupplierFeed.Api.Contracts;
using SupplierFeed.Api.Domain;
using SupplierFeed.Api.Services;

namespace SupplierFeed.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public sealed class ReservationsController : ControllerBase
{
    #region Fields & Dependencies
    private readonly IReservationIngestService _ingestService;
    #endregion

    #region Constructor
    public ReservationsController(IReservationIngestService ingestService)
    {
        _ingestService = ingestService;
    }
    #endregion

    #region Public Methods
    [HttpGet("stats/{supplierId}")]
    public async Task<ActionResult<SupplierStatsResponse>> GetStatsAsync(string supplierId, CancellationToken cancellationToken)
    {
        var stats = await _ingestService.GetSupplierStatsAsync(supplierId, cancellationToken);
        return Ok(stats);
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestAsync([FromBody] ReservationIngestRequest request, CancellationToken cancellationToken)
    {
        var result = await _ingestService.IngestAsync(request, cancellationToken);
        var response = new ReservationIngestResponse { Message = result.Message, Status = result.Outcome };

        switch (result.Outcome)
        {
            case IngestionOutcome.Conflict:
                return Conflict(response);
            case IngestionOutcome.Created:
                return StatusCode(StatusCodes.Status201Created, response);
            case IngestionOutcome.Throttled:
                Response.Headers.RetryAfter = CalculateRetryAfterSeconds(result.RetryAfter).ToString(CultureInfo.InvariantCulture);
                return StatusCode(StatusCodes.Status429TooManyRequests, response);
            default:
                return Ok(response);
        }
    }
    #endregion

    #region Private Helpers
    private static int CalculateRetryAfterSeconds(TimeSpan retryAfter)
    {
        return Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
    }
    #endregion
}
