using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Application.Queries;
using Industrial.Adam.EquipmentScheduling.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.EquipmentScheduling.WebApi.Controllers;

/// <summary>
/// API controller for equipment availability and scheduling
/// </summary>
[ApiController]
[Route("api/equipment-scheduling/[controller]")]
[Produces("application/json")]
[Authorize("RequireOperational")]
public sealed class AvailabilityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AvailabilityController> _logger;

    /// <summary>
    /// Initializes a new instance of the AvailabilityController
    /// </summary>
    /// <param name="mediator">MediatR instance for CQRS operations</param>
    /// <param name="logger">Logger instance</param>
    public AvailabilityController(IMediator mediator, ILogger<AvailabilityController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets equipment availability for a specific resource and date range
    /// </summary>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="startDate">Start date (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date (YYYY-MM-DD format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Equipment availability summary</returns>
    [HttpGet("equipment/{resourceId:long}/availability")]
    [ProducesResponseType<ApiResponse<ScheduleAvailabilityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ScheduleAvailabilityDto>>> GetEquipmentAvailability(
        long resourceId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting equipment availability for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDate.Date, endDate.Date);

        if (startDate > endDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((endDate - startDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 365 days"));
        }

        var query = new GetEquipmentAvailabilityQuery(resourceId, startDate.Date, endDate.Date);

        try
        {
            var availability = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);
            return Ok(ApiResponse<ScheduleAvailabilityDto>.Ok(availability));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Gets equipment schedules for a specific resource and date range
    /// </summary>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="startDate">Start date (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date (YYYY-MM-DD format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of equipment schedules</returns>
    [HttpGet("equipment/{resourceId:long}/schedules")]
    [ProducesResponseType<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EquipmentScheduleDto>>>> GetEquipmentSchedules(
        long resourceId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting equipment schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDate.Date, endDate.Date);

        if (startDate > endDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((endDate - startDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 365 days"));
        }

        var query = new GetEquipmentSchedulesQuery(resourceId, startDate.Date, endDate.Date);
        var schedules = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        return Ok(ApiResponse<IEnumerable<EquipmentScheduleDto>>.Ok(schedules));
    }

    /// <summary>
    /// Gets daily schedule summary for a specific resource and date
    /// </summary>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="date">The date (YYYY-MM-DD format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Daily schedule summary</returns>
    [HttpGet("equipment/{resourceId:long}/daily-summary")]
    [ProducesResponseType<ApiResponse<DailyScheduleSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<DailyScheduleSummaryDto>>> GetDailyScheduleSummary(
        long resourceId,
        [FromQuery] DateTime date,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting daily schedule summary for resource {ResourceId} on {Date}",
            resourceId, date.Date);

        var query = new GetDailyScheduleSummaryQuery(resourceId, date.Date);
        var summary = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        if (summary == null)
        {
            return NotFound(ApiResponse.Failed($"No schedule summary found for resource {resourceId} on {date:yyyy-MM-dd}"));
        }

        return Ok(ApiResponse<DailyScheduleSummaryDto>.Ok(summary));
    }

    /// <summary>
    /// Gets current active schedules across all or specific resources
    /// </summary>
    /// <param name="resourceId">Optional resource ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of currently active schedules</returns>
    [HttpGet("current-active")]
    [ProducesResponseType<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EquipmentScheduleDto>>>> GetCurrentActiveSchedules(
        [FromQuery] long? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting current active schedules, resourceId: {ResourceId}", resourceId);

        var query = new GetCurrentActiveSchedulesQuery(resourceId);
        var schedules = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        return Ok(ApiResponse<IEnumerable<EquipmentScheduleDto>>.Ok(schedules));
    }

    /// <summary>
    /// Gets schedule conflicts for a specific resource and date range
    /// </summary>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="startDate">Start date (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date (YYYY-MM-DD format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedule conflicts</returns>
    [HttpGet("equipment/{resourceId:long}/conflicts")]
    [ProducesResponseType<ApiResponse<IEnumerable<string>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<string>>>> GetScheduleConflicts(
        long resourceId,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting schedule conflicts for resource {ResourceId} from {StartDate} to {EndDate}",
            resourceId, startDate.Date, endDate.Date);

        if (startDate > endDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((endDate - startDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 365 days"));
        }

        var query = new GetScheduleConflictsQuery(resourceId, startDate.Date, endDate.Date);
        var conflicts = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        return Ok(ApiResponse<IEnumerable<string>>.Ok(conflicts));
    }

    /// <summary>
    /// Checks if equipment is currently operating at the specified time
    /// </summary>
    /// <param name="resourceId">The resource ID</param>
    /// <param name="timestamp">The timestamp to check (optional, defaults to current time)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Whether the equipment is operating at the specified time</returns>
    [HttpGet("equipment/{resourceId:long}/is-operating")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> IsOperating(
        long resourceId,
        [FromQuery] DateTime? timestamp = null,
        CancellationToken cancellationToken = default)
    {
        var checkTime = timestamp ?? DateTime.UtcNow;
        _logger.LogDebug("Checking if resource {ResourceId} is operating at {Timestamp}", resourceId, checkTime);

        var query = new GetCurrentActiveSchedulesQuery(resourceId);
        var activeSchedules = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        var isOperating = activeSchedules.Any(s =>
            s.PlannedStartTime <= checkTime &&
            s.PlannedEndTime >= checkTime);

        var response = new
        {
            ResourceId = resourceId,
            Timestamp = checkTime,
            IsOperating = isOperating,
            ActiveSchedules = activeSchedules.Where(s =>
                s.PlannedStartTime <= checkTime &&
                s.PlannedEndTime >= checkTime)
        };

        return Ok(ApiResponse<object>.Ok(response));
    }

    /// <summary>
    /// Gets missing schedules that need to be generated
    /// </summary>
    /// <param name="startDate">Start date (YYYY-MM-DD format)</param>
    /// <param name="endDate">End date (YYYY-MM-DD format)</param>
    /// <param name="resourceId">Optional resource ID to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resource IDs and dates that need schedule generation</returns>
    [HttpGet("missing-schedules")]
    [ProducesResponseType<ApiResponse<IEnumerable<object>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<object>>>> GetMissingSchedules(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] long? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting missing schedules from {StartDate} to {EndDate}, resourceId: {ResourceId}",
            startDate.Date, endDate.Date, resourceId);

        if (startDate > endDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((endDate - startDate).TotalDays > 90)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 90 days for missing schedule queries"));
        }

        var query = new GetMissingSchedulesQuery(startDate.Date, endDate.Date, resourceId);
        var missingSchedules = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

        // Convert tuples to anonymous objects for JSON serialization
        var response = missingSchedules.Select(ms => new
        {
            ResourceId = ms.ResourceId,
            Date = ms.Date
        });

        return Ok(ApiResponse<IEnumerable<object>>.Ok(response));
    }
}
