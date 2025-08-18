using Industrial.Adam.EquipmentScheduling.Application.Commands;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.EquipmentScheduling.WebApi.Controllers;

/// <summary>
/// API controller for schedule management and generation
/// </summary>
[ApiController]
[Route("api/equipment-scheduling/[controller]")]
[Produces("application/json")]
public sealed class SchedulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SchedulesController> _logger;

    public SchedulesController(IMediator mediator, ILogger<SchedulesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates schedules for a resource within a date range
    /// </summary>
    /// <param name="command">The generate schedules command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of generated schedules</returns>
    [HttpPost("generate")]
    [ProducesResponseType<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EquipmentScheduleDto>>>> GenerateSchedules(
        [FromBody] GenerateSchedulesCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            command.ResourceId, command.StartDate, command.EndDate);

        if (command.StartDate > command.EndDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((command.EndDate - command.StartDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 365 days"));
        }

        try
        {
            var schedules = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(AvailabilityController.GetEquipmentSchedules),
                "Availability",
                new { resourceId = command.ResourceId, startDate = command.StartDate, endDate = command.EndDate },
                ApiResponse<IEnumerable<EquipmentScheduleDto>>.Ok(schedules));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Regenerates schedules for a resource within a date range (deletes existing and creates new)
    /// </summary>
    /// <param name="command">The regenerate schedules command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of regenerated schedules</returns>
    [HttpPost("regenerate")]
    [ProducesResponseType<ApiResponse<IEnumerable<EquipmentScheduleDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<EquipmentScheduleDto>>>> RegenerateSchedules(
        [FromBody] RegenerateSchedulesCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Regenerating schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            command.ResourceId, command.StartDate, command.EndDate);

        if (command.StartDate > command.EndDate)
        {
            return BadRequest(ApiResponse.Failed("Start date cannot be after end date"));
        }

        if ((command.EndDate - command.StartDate).TotalDays > 365)
        {
            return BadRequest(ApiResponse.Failed("Date range cannot exceed 365 days"));
        }

        try
        {
            var schedules = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<IEnumerable<EquipmentScheduleDto>>.Ok(schedules));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Creates an exception schedule for a resource
    /// </summary>
    /// <param name="command">The create exception schedule command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created exception schedule</returns>
    [HttpPost("exception")]
    [ProducesResponseType<ApiResponse<EquipmentScheduleDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<EquipmentScheduleDto>>> CreateExceptionSchedule(
        [FromBody] CreateExceptionScheduleCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating exception schedule for resource {ResourceId} on {Date}",
            command.ResourceId, command.Date);

        if (command.StartTime >= command.EndTime)
        {
            return BadRequest(ApiResponse.Failed("Start time must be before end time"));
        }

        if (command.Date.Date != command.StartTime.Date || command.Date.Date != command.EndTime.Date)
        {
            return BadRequest(ApiResponse.Failed("Start time and end time must be on the same date as the schedule date"));
        }

        try
        {
            var schedule = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetSchedule),
                new { id = schedule.Id },
                ApiResponse<EquipmentScheduleDto>.Ok(schedule));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("conflicts"))
        {
            return Conflict(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Gets a specific schedule by ID
    /// </summary>
    /// <param name="id">The schedule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The equipment schedule</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<EquipmentScheduleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EquipmentScheduleDto>>> GetSchedule(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting schedule {ScheduleId}", id);

        // This would need a GetScheduleByIdQuery which we haven't implemented yet
        // For now, return a placeholder response
        return NotFound(ApiResponse.Failed($"Schedule with ID {id} not found"));
    }

    /// <summary>
    /// Updates an equipment schedule
    /// </summary>
    /// <param name="id">The schedule ID</param>
    /// <param name="command">The update command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated schedule</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType<ApiResponse<EquipmentScheduleDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<EquipmentScheduleDto>>> UpdateSchedule(
        long id,
        [FromBody] UpdateEquipmentScheduleCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating schedule {ScheduleId}", id);

        if (command.PlannedHours <= 0)
        {
            return BadRequest(ApiResponse.Failed("Planned hours must be greater than zero"));
        }

        if (command.PlannedStartTime.HasValue && command.PlannedEndTime.HasValue &&
            command.PlannedStartTime >= command.PlannedEndTime)
        {
            return BadRequest(ApiResponse.Failed("Start time must be before end time"));
        }

        try
        {
            // Create a new command with the ID
            var updateCommand = command with { Id = id };
            var schedule = await _mediator.Send(updateCommand, cancellationToken);
            return Ok(ApiResponse<EquipmentScheduleDto>.Ok(schedule));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Cancels an equipment schedule
    /// </summary>
    /// <param name="id">The schedule ID</param>
    /// <param name="reason">Optional cancellation reason</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> CancelSchedule(
        long id,
        [FromBody] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling schedule {ScheduleId}", id);

        try
        {
            var command = new CancelEquipmentScheduleCommand(id, reason);
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Ok("Schedule cancelled successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Completes an equipment schedule
    /// </summary>
    /// <param name="id">The schedule ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:long}/complete")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> CompleteSchedule(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing schedule {ScheduleId}", id);

        try
        {
            var command = new CompleteEquipmentScheduleCommand(id);
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Ok("Schedule completed successfully"));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
    }
}
