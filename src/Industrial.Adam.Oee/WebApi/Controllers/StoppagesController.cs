using System.ComponentModel.DataAnnotations;
using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Controller for stoppage detection and management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize("RequireOperational")]
public class StoppagesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StoppagesController> _logger;

    /// <summary>
    /// Constructor for Stoppages controller
    /// </summary>
    /// <param name="mediator">MediatR instance for CQRS</param>
    /// <param name="logger">Logger instance</param>
    public StoppagesController(IMediator mediator, ILogger<StoppagesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current stoppage information for a specific device
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <param name="minimumMinutes">Minimum stoppage duration in minutes to be considered (default: 5)</param>
    /// <returns>Current stoppage information if device is stopped</returns>
    /// <response code="200">Returns current stoppage information</response>
    /// <response code="204">No stoppage detected - device is running normally</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Device not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(StoppageInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StoppageInfoDto>> GetCurrentStoppage(
        [FromQuery, Required] string deviceId,
        [FromQuery, Range(1, 60)] int minimumMinutes = 5)
    {
        try
        {
            _logger.LogInformation("Checking for current stoppage on device {DeviceId} (minimum {MinimumMinutes} minutes)",
                deviceId, minimumMinutes);

            var query = new GetCurrentStoppageQuery(deviceId, minimumMinutes);
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogInformation("No current stoppage detected for device {DeviceId}", deviceId);
                return NoContent(); // Device is running normally
            }

            _logger.LogInformation("Current stoppage detected for device {DeviceId}: {DurationMinutes} minutes active",
                deviceId, result.DurationMinutes);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for current stoppage query: {DeviceId}", deviceId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get historical stoppage data for a specific device over a time period
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <param name="period">Number of hours to look back from current time (default: 24)</param>
    /// <param name="startTime">Optional start time for custom date range</param>
    /// <param name="endTime">Optional end time for custom date range</param>
    /// <param name="minimumMinutes">Minimum stoppage duration in minutes to include (default: 5)</param>
    /// <returns>Historical stoppage data</returns>
    /// <response code="200">Returns historical stoppage data</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Device not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<StoppageInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<StoppageInfoDto>>> GetStoppageHistory(
        [FromQuery, Required] string deviceId,
        [FromQuery, Range(1, 8760)] int period = 24,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery, Range(1, 60)] int minimumMinutes = 5)
    {
        try
        {
            _logger.LogInformation("Retrieving stoppage history for device {DeviceId} over {Period} hours",
                deviceId, period);

            var query = new GetStoppageHistoryQuery(deviceId, period, minimumMinutes)
            {
                StartTime = startTime,
                EndTime = endTime
            };

            var result = await _mediator.Send(query);

            _logger.LogInformation("Retrieved {Count} stoppage periods for device {DeviceId}",
                result.Count(), deviceId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for stoppage history: {DeviceId}", deviceId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Classify a stoppage with a reason code (placeholder for future implementation)
    /// </summary>
    /// <param name="id">Stoppage identifier</param>
    /// <param name="classification">Classification details</param>
    /// <returns>Updated stoppage information</returns>
    /// <response code="200">Stoppage classified successfully</response>
    /// <response code="400">Invalid classification data</response>
    /// <response code="404">Stoppage not found</response>
    /// <response code="501">Not implemented - placeholder for future functionality</response>
    [HttpPut("{id}/classify")]
    [ProducesResponseType(typeof(StoppageInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status501NotImplemented)]
    public Task<ActionResult<StoppageInfoDto>> ClassifyStoppage(
        [FromRoute, Required] string id,
        [FromBody] object classification)
    {
        _logger.LogInformation("Stoppage classification requested for ID {StoppageId}", id);

        // This endpoint is included in the API specification but not yet implemented
        // It would require additional domain modeling for stoppage classification
        return Task.FromResult<ActionResult<StoppageInfoDto>>(StatusCode(StatusCodes.Status501NotImplemented, new ProblemDetails
        {
            Title = "Not Implemented",
            Detail = "Stoppage classification functionality is not yet implemented. This endpoint is reserved for future development.",
            Status = StatusCodes.Status501NotImplemented,
            Instance = HttpContext.Request.Path
        }));
    }
}
