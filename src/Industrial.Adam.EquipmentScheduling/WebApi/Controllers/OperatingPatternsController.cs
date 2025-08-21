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

    /// <summary>
    /// Initializes a new instance of the OperatingPatternsController
    /// </summary>
    /// <param name="mediator">MediatR instance for CQRS operations</param>
    /// <param name="logger">Logger instance</param>
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

        try
        {
            var query = new GetVisibleOperatingPatternsQuery();
            var patterns = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {PatternCount} visible operating patterns", patterns.Count());
            return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve visible operating patterns");
            throw;
        }
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

        try
        {
            var query = new GetOperatingPatternByIdQuery(id);
            var pattern = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            if (pattern == null)
            {
                _logger.LogWarning("Operating pattern {PatternId} not found", id);
                return NotFound(ApiResponse.Failed($"Operating pattern with ID {id} not found"));
            }

            _logger.LogDebug("Successfully retrieved operating pattern {PatternId}", id);
            return Ok(ApiResponse<OperatingPatternDto>.Ok(pattern));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve operating pattern {PatternId}", id);
            throw;
        }
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

        try
        {
            var query = new GetOperatingPatternsByTypeQuery(type, visibleOnly);
            var patterns = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {PatternCount} operating patterns of type {Type}",
                patterns.Count(), type);
            return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve operating patterns by type {Type}, visibleOnly: {VisibleOnly}",
                type, visibleOnly);
            throw;
        }
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
            _logger.LogWarning("Invalid hours range specified: {MinHours}-{MaxHours}", minHours, maxHours);
            return BadRequest(ApiResponse.Failed("Invalid hours range specified"));
        }

        try
        {
            var query = new GetOperatingPatternsByHoursRangeQuery(minHours, maxHours, visibleOnly);
            var patterns = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {PatternCount} operating patterns in hours range {MinHours}-{MaxHours}",
                patterns.Count(), minHours, maxHours);
            return Ok(ApiResponse<IEnumerable<OperatingPatternDto>>.Ok(patterns));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve operating patterns by hours range {MinHours}-{MaxHours}, visibleOnly: {VisibleOnly}",
                minHours, maxHours, visibleOnly);
            throw;
        }
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

        try
        {
            var query = new GetPatternAvailabilityQuery(id);
            var availability = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            if (availability == null)
            {
                _logger.LogWarning("Pattern availability not found for pattern {PatternId}", id);
                return NotFound(ApiResponse.Failed($"Operating pattern with ID {id} not found"));
            }

            _logger.LogDebug("Successfully retrieved pattern availability for pattern {PatternId}", id);
            return Ok(ApiResponse<PatternAvailabilityDto>.Ok(availability));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve pattern availability for pattern {PatternId}", id);
            throw;
        }
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

        try
        {
            var query = new GetAllPatternAvailabilitiesQuery(visibleOnly);
            var availabilities = await _mediator.Send(query, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully retrieved {AvailabilityCount} pattern availabilities",
                availabilities.Count());
            return Ok(ApiResponse<IEnumerable<PatternAvailabilityDto>>.Ok(availabilities));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all pattern availabilities, visibleOnly: {VisibleOnly}", visibleOnly);
            throw;
        }
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
        if (command == null)
        {
            _logger.LogWarning("Create operating pattern command is null");
            return BadRequest(ApiResponse.Failed("Command cannot be null"));
        }

        _logger.LogInformation("Creating operating pattern {Name}", command.Name);

        try
        {
            var pattern = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Successfully created operating pattern {PatternId} with name {Name}",
                pattern.Id, pattern.Name);

            return CreatedAtAction(
                nameof(GetOperatingPattern),
                new { id = pattern.Id },
                ApiResponse<OperatingPatternDto>.Ok(pattern));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid argument when creating operating pattern {Name}", command.Name);
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating operating pattern {Name}", command.Name);
            return BadRequest(ApiResponse.Failed(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create operating pattern {Name}", command.Name);
            throw;
        }
    }
}
