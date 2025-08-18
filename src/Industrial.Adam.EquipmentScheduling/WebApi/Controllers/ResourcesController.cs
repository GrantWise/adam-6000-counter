using Industrial.Adam.EquipmentScheduling.Application.Commands;
using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Application.Queries;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.WebApi.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Industrial.Adam.EquipmentScheduling.WebApi.Controllers;

/// <summary>
/// API controller for resource management
/// </summary>
[ApiController]
[Route("api/equipment-scheduling/[controller]")]
[Produces("application/json")]
public sealed class ResourcesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(IMediator mediator, ILogger<ResourcesController> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a resource by ID
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resource details</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ApiResponse<ResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResourceDto>>> GetResource(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resource {ResourceId}", id);

        var query = new GetResourceByIdQuery(id);
        var resource = await _mediator.Send(query, cancellationToken);

        if (resource == null)
        {
            return NotFound(ApiResponse.Failed($"Resource with ID {id} not found"));
        }

        return Ok(ApiResponse<ResourceDto>.Ok(resource));
    }

    /// <summary>
    /// Gets a resource by code
    /// </summary>
    /// <param name="code">The resource code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resource details</returns>
    [HttpGet("by-code/{code}")]
    [ProducesResponseType<ApiResponse<ResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResourceDto>>> GetResourceByCode(
        string code,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resource by code {Code}", code);

        var query = new GetResourceByCodeQuery(code);
        var resource = await _mediator.Send(query, cancellationToken);

        if (resource == null)
        {
            return NotFound(ApiResponse.Failed($"Resource with code '{code}' not found"));
        }

        return Ok(ApiResponse<ResourceDto>.Ok(resource));
    }

    /// <summary>
    /// Gets resources by type
    /// </summary>
    /// <param name="type">The resource type</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources</returns>
    [HttpGet("by-type/{type}")]
    [ProducesResponseType<ApiResponse<IEnumerable<ResourceDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ResourceDto>>>> GetResourcesByType(
        ResourceType type,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting resources by type {Type}, activeOnly: {ActiveOnly}", type, activeOnly);

        var query = new GetResourcesByTypeQuery(type, activeOnly);
        var resources = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<ResourceDto>>.Ok(resources));
    }

    /// <summary>
    /// Gets child resources for a parent resource
    /// </summary>
    /// <param name="parentId">The parent resource ID</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child resources</returns>
    [HttpGet("{parentId:long}/children")]
    [ProducesResponseType<ApiResponse<IEnumerable<ResourceDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ResourceDto>>>> GetChildResources(
        long parentId,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting child resources for parent {ParentId}, activeOnly: {ActiveOnly}", parentId, activeOnly);

        var query = new GetChildResourcesQuery(parentId, activeOnly);
        var resources = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<ResourceDto>>.Ok(resources));
    }

    /// <summary>
    /// Gets resource hierarchy
    /// </summary>
    /// <param name="rootId">Optional root resource ID</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resource hierarchy tree</returns>
    [HttpGet("hierarchy")]
    [ProducesResponseType<ApiResponse<IEnumerable<ResourceHierarchyDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ResourceHierarchyDto>>>> GetResourceHierarchy(
        [FromQuery] long? rootId = null,
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting resource hierarchy, rootId: {RootId}, activeOnly: {ActiveOnly}", rootId, activeOnly);

        var query = new GetResourceHierarchyQuery(rootId, activeOnly);
        var hierarchy = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<ResourceHierarchyDto>>.Ok(hierarchy));
    }

    /// <summary>
    /// Gets schedulable resources
    /// </summary>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of schedulable resources</returns>
    [HttpGet("schedulable")]
    [ProducesResponseType<ApiResponse<IEnumerable<ResourceDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ResourceDto>>>> GetSchedulableResources(
        [FromQuery] bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting schedulable resources, activeOnly: {ActiveOnly}", activeOnly);

        var query = new GetSchedulableResourcesQuery(activeOnly);
        var resources = await _mediator.Send(query, cancellationToken);

        return Ok(ApiResponse<IEnumerable<ResourceDto>>.Ok(resources));
    }

    /// <summary>
    /// Creates a new resource
    /// </summary>
    /// <param name="request">The resource creation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created resource</returns>
    [HttpPost]
    [ProducesResponseType<ApiResponse<ResourceDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ResourceDto>>> CreateResource(
        [FromBody] CreateResourceDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating resource with code {Code}", request.Code);

        var command = new CreateResourceCommand(
            request.Name,
            request.Code,
            request.Type,
            request.ParentId,
            request.RequiresScheduling,
            request.Description);

        try
        {
            var resource = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetResource), new { id = resource.Id }, ApiResponse<ResourceDto>.Ok(resource));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing resource
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="request">The resource update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated resource</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType<ApiResponse<ResourceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ResourceDto>>> UpdateResource(
        long id,
        [FromBody] UpdateResourceDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating resource {ResourceId}", id);

        var command = new UpdateResourceCommand(
            id,
            request.Name,
            request.RequiresScheduling,
            request.Description);

        try
        {
            var resource = await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse<ResourceDto>.Ok(resource));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Sets the parent resource
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="parentId">The parent resource ID (null to remove parent)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPut("{id:long}/parent")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> SetResourceParent(
        long id,
        [FromBody] long? parentId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting parent for resource {ResourceId} to {ParentId}", id, parentId);

        var command = new SetResourceParentCommand(id, parentId);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Ok());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Activates a resource
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:long}/activate")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> ActivateResource(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating resource {ResourceId}", id);

        var command = new ActivateResourceCommand(id);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Ok());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
    }

    /// <summary>
    /// Deactivates a resource
    /// </summary>
    /// <param name="id">The resource ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success response</returns>
    [HttpPost("{id:long}/deactivate")]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> DeactivateResource(
        long id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating resource {ResourceId}", id);

        var command = new DeactivateResourceCommand(id);

        try
        {
            await _mediator.Send(command, cancellationToken);
            return Ok(ApiResponse.Ok());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ApiResponse.Failed(ex.Message));
        }
    }
}
