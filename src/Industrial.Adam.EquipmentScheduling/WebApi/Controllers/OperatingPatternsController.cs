using Industrial.Adam.EquipmentScheduling.Application.Commands;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Application.Queries;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.EquipmentScheduling.WebApi.Controllers;

/// <summary>
/// API controller for operating patterns management
/// </summary>
[ApiController]
[Route("api/equipment-scheduling/[controller]")]
[Produces("application/json")]
public sealed class OperatingPatternsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OperatingPatternsController> _logger;

    public OperatingPatternsController(IMediator mediator, ILogger<OperatingPatternsController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets all visible operating patterns
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of visible operating patterns</returns>
    [HttpGet]
    [ProducesResponseType<ApiResponse<IEnumerable<OperatingPatternDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OperatingPatternDto>>>> GetOperatingPatterns(
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all visible operating patterns");

        var query = new GetVisibleOperatingPatternsQuery();
        var patterns = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
    }

    /// <summary>
    /// Gets an operating pattern by ID
    /// </summary>
    /// <param name="id">The pattern ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The operating pattern</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ApiResponse<OperatingPatternDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<OperatingPatternDto>>> GetOperatingPattern(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting operating pattern {PatternId}", id);

        var query = new GetOperatingPatternByIdQuery(id);
        var pattern = await _mediator.Send(query, cancellationToken);

        if (pattern == null)
        {
            return NotFound(ApiResponse.Failed($"Operating pattern with ID {id} not found"));
        }

        return Ok(ApiResponse<OperatingPatternDto>.Ok(pattern));
    }

    /// <summary>
    /// Gets operating patterns by type
    /// </summary>
    /// <param name="type">The pattern type</param>
    /// <param name="visibleOnly">Whether to include only visible patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operating patterns</returns>
    [HttpGet("by-type/{type}")]
    [ProducesResponseType<ApiResponse<IEnumerable<OperatingPatternDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OperatingPatternDto>>>> GetOperatingPatternsByType(
        PatternType type,
        [FromQuery] bool visibleOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting operating patterns by type {Type}, visibleOnly: {VisibleOnly}", type, visibleOnly);

        var query = new GetOperatingPatternsByTypeQuery(type, visibleOnly);
        var patterns = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
    }

    /// <summary>
    /// Gets operating patterns by weekly hours range
    /// </summary>
    /// <param name="minHours">Minimum weekly hours</param>
    /// <param name="maxHours">Maximum weekly hours</param>
    /// <param name="visibleOnly">Whether to include only visible patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of operating patterns</returns>
    [HttpGet("by-hours")]
    [ProducesResponseType<ApiResponse<IEnumerable<OperatingPatternDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IEnumerable<OperatingPatternDto>>>> GetOperatingPatternsByHours(
        [FromQuery] decimal minHours,
        [FromQuery] decimal maxHours,
        [FromQuery] bool visibleOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting operating patterns by hours range {MinHours}-{MaxHours}, visibleOnly: {VisibleOnly}",
            minHours, maxHours, visibleOnly);

        if (minHours < 0 || maxHours < minHours)
        {
            return BadRequest(ApiResponse.Failed("Invalid hours range specified"));
        }

        var query = new GetOperatingPatternsByHoursRangeQuery(minHours, maxHours, visibleOnly);
        var patterns = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
    }

    /// <summary>
    /// Gets pattern availability information
    /// </summary>
    /// <param name="id">The pattern ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Pattern availability information</returns>
    [HttpGet("{id:int}/availability")]
    [ProducesResponseType<ApiResponse<PatternAvailabilityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PatternAvailabilityDto>>> GetPatternAvailability(
        int id,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting pattern availability for pattern {PatternId}", id);

        var query = new GetPatternAvailabilityQuery(id);
        var availability = await _mediator.Send(query, cancellationToken);

        if (availability == null)
        {
            return NotFound(ApiResponse.Failed($"Operating pattern with ID {id} not found"));
        }

        return Ok(ApiResponse<PatternAvailabilityDto>.Ok(availability));
    }

    /// <summary>
    /// Gets all pattern availabilities
    /// </summary>
    /// <param name="visibleOnly">Whether to include only visible patterns</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of pattern availability information</returns>
    [HttpGet("availabilities")]
    [ProducesResponseType<ApiResponse<IEnumerable<PatternAvailabilityDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PatternAvailabilityDto>>>> GetAllPatternAvailabilities(
        [FromQuery] bool visibleOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all pattern availabilities, visibleOnly: {VisibleOnly}", visibleOnly);

        var query = new GetAllPatternAvailabilitiesQuery(visibleOnly);
        var availabilities = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<PatternAvailabilityDto>>.Ok(availabilities));
    }

    /// <summary>
    /// Creates a new operating pattern
    /// </summary>
    /// <param name="command">The create command</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created operating pattern</returns>
    [HttpPost]
    [ProducesResponseType<ApiResponse<OperatingPatternDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<OperatingPatternDto>>> CreateOperatingPattern(
        [FromBody] CreateOperatingPatternCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating operating pattern {Name}", command.Name);

        try
        {
            var pattern = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(
                nameof(GetOperatingPattern),
                new { id = pattern.Id },
                ApiResponse<OperatingPatternDto>.Ok(pattern));
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
}
