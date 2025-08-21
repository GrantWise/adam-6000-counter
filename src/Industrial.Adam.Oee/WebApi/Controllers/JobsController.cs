using System.ComponentModel.DataAnnotations;
using Industrial.Adam.Oee.Application.Commands;
using Industrial.Adam.Oee.Application.DTOs;
using Industrial.Adam.Oee.Application.Queries;
using Industrial.Adam.Oee.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.Oee.WebApi.Controllers;

/// <summary>
/// Controller for work order (job) management operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize("RequireProduction")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<JobsController> _logger;

    /// <summary>
    /// Constructor for Jobs controller
    /// </summary>
    /// <param name="mediator">MediatR instance for CQRS</param>
    /// <param name="logger">Logger instance</param>
    public JobsController(IMediator mediator, ILogger<JobsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get the active work order for a specific device
    /// </summary>
    /// <param name="deviceId">Device/resource identifier</param>
    /// <returns>Active work order information</returns>
    /// <response code="200">Returns active work order</response>
    /// <response code="404">No active work order found for device</response>
    /// <response code="400">Invalid device ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("active")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkOrderDto>> GetActiveJob(
        [FromQuery, Required] string deviceId)
    {
        try
        {
            _logger.LogInformation("Retrieving active work order for device {DeviceId}", deviceId);

            var query = new GetActiveWorkOrderQuery { DeviceId = deviceId };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogInformation("No active work order found for device {DeviceId}", deviceId);
                return NotFound(new ProblemDetails
                {
                    Title = "No Active Work Order",
                    Detail = $"No active work order found for device {deviceId}",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Retrieved active work order {WorkOrderId} for device {DeviceId}",
                result.WorkOrderId, deviceId);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid device ID: {DeviceId}", deviceId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Device ID",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get a specific work order by ID
    /// </summary>
    /// <param name="id">Work order identifier</param>
    /// <returns>Work order information</returns>
    /// <response code="200">Returns work order details</response>
    /// <response code="404">Work order not found</response>
    /// <response code="400">Invalid work order ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(WorkOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkOrderDto>> GetWorkOrder(
        [FromRoute, Required] string id)
    {
        try
        {
            _logger.LogInformation("Retrieving work order {WorkOrderId}", id);

            var query = new GetWorkOrderQuery { WorkOrderId = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogInformation("Work order {WorkOrderId} not found", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Work Order Not Found",
                    Detail = $"Work order with ID {id} was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Retrieved work order {WorkOrderId}", id);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid work order ID: {WorkOrderId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Work Order ID",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Get progress information for a specific work order
    /// </summary>
    /// <param name="id">Work order identifier</param>
    /// <returns>Work order progress details</returns>
    /// <response code="200">Returns work order progress</response>
    /// <response code="404">Work order not found</response>
    /// <response code="400">Invalid work order ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/progress")]
    [ProducesResponseType(typeof(WorkOrderProgressDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<WorkOrderProgressDto>> GetWorkOrderProgress(
        [FromRoute, Required] string id)
    {
        try
        {
            _logger.LogInformation("Retrieving progress for work order {WorkOrderId}", id);

            var query = new GetWorkOrderProgressQuery { WorkOrderId = id };
            var result = await _mediator.Send(query);

            if (result == null)
            {
                _logger.LogInformation("Work order {WorkOrderId} not found for progress query", id);
                return NotFound(new ProblemDetails
                {
                    Title = "Work Order Not Found",
                    Detail = $"Work order with ID {id} was not found",
                    Status = StatusCodes.Status404NotFound,
                    Instance = HttpContext.Request.Path
                });
            }

            _logger.LogInformation("Retrieved progress for work order {WorkOrderId}: {CompletionPercent}% complete",
                id, result.CompletionPercentage);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid work order ID for progress: {WorkOrderId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Work Order ID",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Start a new work order
    /// </summary>
    /// <param name="request">Work order creation details</param>
    /// <returns>Created work order ID</returns>
    /// <response code="201">Work order created successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="409">Work order already exists or device has active work order</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<string>> StartWorkOrder(
        [FromBody] StartWorkOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Starting work order {WorkOrderId} for device {DeviceId}",
                request.WorkOrderId, request.DeviceId);

            var command = new StartWorkOrderCommand(
                request.WorkOrderId,
                request.WorkOrderDescription,
                request.ProductId,
                request.ProductDescription,
                request.PlannedQuantity,
                request.ScheduledStartTime,
                request.ScheduledEndTime,
                request.DeviceId,
                request.UnitOfMeasure,
                request.OperatorId);

            var workOrderId = await _mediator.Send(command);

            _logger.LogInformation("Successfully started work order {WorkOrderId} for device {DeviceId}",
                workOrderId, request.DeviceId);

            return CreatedAtAction(
                nameof(GetWorkOrder),
                new { id = workOrderId },
                workOrderId);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid data for starting work order: {WorkOrderId}", request.WorkOrderId);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Work Order Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot start work order {WorkOrderId} - conflict detected", request.WorkOrderId);
            return Conflict(new ProblemDetails
            {
                Title = "Work Order Conflict",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Instance = HttpContext.Request.Path
            });
        }
    }

    /// <summary>
    /// Complete an active work order
    /// </summary>
    /// <param name="id">Work order identifier to complete</param>
    /// <param name="request">Completion details</param>
    /// <returns>No content on successful completion</returns>
    /// <response code="204">Work order completed successfully</response>
    /// <response code="400">Invalid request data</response>
    /// <response code="404">Work order not found</response>
    /// <response code="409">Work order already completed or cannot be completed</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteWorkOrder(
        [FromRoute, Required] string id,
        [FromBody] CompleteWorkOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Completing work order {WorkOrderId} with {GoodQuantity} good and {ScrapQuantity} scrap",
                id, request.ActualQuantityGood, request.ActualQuantityScrap);

            var command = new CompleteWorkOrderCommand(
                id,
                request.CompletionNotes,
                request.ActualQuantityGood,
                request.ActualQuantityScrap,
                request.CompletedByOperatorId
            );

            await _mediator.Send(command);

            _logger.LogInformation("Successfully completed work order {WorkOrderId}", id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid data for completing work order: {WorkOrderId}", id);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid Completion Data",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            _logger.LogWarning(ex, "Work order {WorkOrderId} not found for completion", id);
            return NotFound(new ProblemDetails
            {
                Title = "Work Order Not Found",
                Detail = $"Work order with ID {id} was not found",
                Status = StatusCodes.Status404NotFound,
                Instance = HttpContext.Request.Path
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot complete work order {WorkOrderId}", id);
            return Conflict(new ProblemDetails
            {
                Title = "Cannot Complete Work Order",
                Detail = ex.Message,
                Status = StatusCodes.Status409Conflict,
                Instance = HttpContext.Request.Path
            });
        }
    }
}
