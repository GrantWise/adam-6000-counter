using Industrial.Adam.AdminDashboard.Application.Commands;
using Industrial.Adam.AdminDashboard.Application.DTOs;
using Industrial.Adam.AdminDashboard.Application.Queries;
using Industrial.Adam.Security.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.AdminDashboard.WebApi.Controllers;

/// <summary>
/// Admin dashboard API controller for system health monitoring and alerts
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Roles = $"{RoleConstants.SystemAdmin},{RoleConstants.Admin}")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    /// <summary>
    /// Constructor for AdminController
    /// </summary>
    /// <param name="mediator">MediatR instance</param>
    /// <param name="logger">Logger instance</param>
    public AdminController(IMediator mediator, ILogger<AdminController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get system health status for all services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of service health status</returns>
    /// <response code="200">Returns service health status</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="403">Forbidden - insufficient privileges</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("health/services")]
    [ProducesResponseType(typeof(IEnumerable<ServiceHealthDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ServiceHealthDto>>> GetSystemHealth(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting system health status for all services");
        
        var query = new GetSystemHealthQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Get system performance metrics (CPU, RAM, disk)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System performance metrics</returns>
    /// <response code="200">Returns system metrics</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="403">Forbidden - insufficient privileges</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("health/metrics")]
    [ProducesResponseType(typeof(SystemMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SystemMetricsDto>> GetSystemMetrics(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting system performance metrics");
        
        var query = new GetSystemMetricsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Get database health metrics and connection information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database health information</returns>
    /// <response code="200">Returns database health status</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="403">Forbidden - insufficient privileges</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("health/database")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetDatabaseHealth(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting database health metrics");
        
        var query = new GetDatabaseHealthQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Get active system alerts
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active alerts</returns>
    /// <response code="200">Returns active alerts</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="403">Forbidden - insufficient privileges</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(IEnumerable<SystemAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SystemAlertDto>>> GetActiveAlerts(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting active system alerts");
        
        var query = new GetActiveAlertsQuery();
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }

    /// <summary>
    /// Acknowledge a system alert
    /// </summary>
    /// <param name="alertId">Alert ID to acknowledge</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success status</returns>
    /// <response code="200">Alert acknowledged successfully</response>
    /// <response code="400">Invalid alert ID</response>
    /// <response code="401">Unauthorized access</response>
    /// <response code="403">Forbidden - insufficient privileges</response>
    /// <response code="404">Alert not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("alerts/{alertId}/acknowledge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> AcknowledgeAlert(int alertId, CancellationToken cancellationToken = default)
    {
        if (alertId <= 0)
        {
            return BadRequest(new { Error = "Invalid alert ID" });
        }

        // Get user ID from claims (simplified for demo)
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value ?? "system";

        _logger.LogInformation("User {UserId} acknowledging alert {AlertId}", userId, alertId);
        
        var command = new AcknowledgeAlertCommand
        {
            AlertId = alertId,
            AcknowledgedBy = userId
        };
        
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result)
        {
            return Ok(new { Message = "Alert acknowledged successfully" });
        }
        else
        {
            return NotFound(new { Error = "Alert not found or already acknowledged" });
        }
    }

    /// <summary>
    /// Health check endpoint for this service
    /// </summary>
    /// <returns>Health status</returns>
    /// <response code="200">Service is healthy</response>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetHealth()
    {
        var response = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "AdminDashboard API",
            Version = "1.0.0"
        };

        return Ok(response);
    }
}

