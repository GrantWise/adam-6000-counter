using System.ComponentModel.DataAnnotations;
using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Controller for OEE (Overall Equipment Effectiveness) metrics and calculations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize("RequireOperational")]
public class OeeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OeeController> _logger;

    /// <summary>
    /// Constructor for OEE controller
    /// </summary>
    /// <param name="mediator">MediatR instance for CQRS</param>
    /// <param name="logger">Logger instance</param>
    public OeeController(IMediator mediator, ILogger<OeeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get current OEE metrics for a specific device
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <param name="startTime">Optional start time for calculation period (ISO 8601)</param>
    /// <param name="endTime">Optional end time for calculation period (ISO 8601)</param>
    /// <returns>Current OEE metrics</returns>
    /// <response code="200">Returns current OEE metrics</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Device not found or no active work order</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(OeeCalculationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OeeCalculationDto>> GetCurrentOee(
        [FromQuery, Required] string deviceId,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            _logger.LogInformation("Calculating current OEE for device {DeviceId} from {StartTime} to {EndTime}",
                deviceId, startTime, endTime);

            var query = new CalculateCurrentOeeQuery(deviceId, startTime, endTime);
            var result = await _mediator.Send(query);

            _logger.LogInformation("Successfully calculated OEE for device {DeviceId}: {OeePercent}%",
                deviceId, result.OeePercentage);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for OEE calculation: {DeviceId}", deviceId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No active work order found for device {DeviceId}", deviceId);
            return NotFound(new ProblemDetails
            {
                Title = "No Active Work Order",
                Detail = $"No active work order found for device {deviceId}",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get historical OEE data for a specific device over a time period
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <param name="period">Number of hours to look back from current time</param>
    /// <param name="startTime">Optional start time for custom date range</param>
    /// <param name="endTime">Optional end time for custom date range</param>
    /// <param name="intervalMinutes">Data aggregation interval in minutes (default: 60)</param>
    /// <returns>Historical OEE data points</returns>
    /// <response code="200">Returns historical OEE data</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Device not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IEnumerable<OeeCalculationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OeeCalculationDto>>> GetOeeHistory(
        [FromQuery, Required] string deviceId,
        [FromQuery, Range(1, 8760)] int period = 24,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery, Range(1, 1440)] int intervalMinutes = 60)
    {
        try
        {
            _logger.LogInformation("Retrieving OEE history for device {DeviceId} over {Period} hours",
                deviceId, period);

            // Determine the actual time range
            var actualEndTime = endTime ?? DateTime.UtcNow;
            var actualStartTime = startTime ?? actualEndTime.AddHours(-period);

            var query = new GetOeeHistoryQuery(deviceId, actualStartTime, actualEndTime);

            var result = await _mediator.Send(query);

            _logger.LogInformation("Successfully retrieved {Count} OEE history points for device {DeviceId}",
                result.Count(), deviceId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for OEE history: {DeviceId}", deviceId);
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
    /// Get detailed OEE breakdown showing availability, performance, and quality factors
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <param name="startTime">Optional start time for breakdown period</param>
    /// <param name="endTime">Optional end time for breakdown period</param>
    /// <returns>Detailed OEE breakdown with factor analysis</returns>
    /// <response code="200">Returns detailed OEE breakdown</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="404">Device not found or no data available</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("breakdown")]
    [ProducesResponseType(typeof(OeeCalculationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OeeCalculationDto>> GetOeeBreakdown(
        [FromQuery, Required] string deviceId,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            _logger.LogInformation("Retrieving OEE breakdown for device {DeviceId}", deviceId);

            // For breakdown, we want the same calculation as current but with detailed analysis
            var query = new CalculateCurrentOeeQuery(deviceId, startTime, endTime);
            var result = await _mediator.Send(query);

            _logger.LogInformation("Successfully retrieved OEE breakdown for device {DeviceId} - " +
                "Availability: {Availability}%, Performance: {Performance}%, Quality: {Quality}%",
                deviceId, result.AvailabilityPercentage, result.PerformancePercentage, result.QualityPercentage);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid parameters for OEE breakdown: {DeviceId}", deviceId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Parameters",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No data available for OEE breakdown: {DeviceId}", deviceId);
            return NotFound(new ProblemDetails
            {
                Title = "No Data Available",
                Detail = $"No OEE data available for device {deviceId} in the specified time period",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
