using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands.Handlers;

/// <summary>
/// Handler for creating resource commands
/// </summary>
public sealed class CreateResourceCommandHandler :
    IRequestHandler<CreateResourceCommand, ResourceDto>,
    IRequestHandler<UpdateResourceCommand, ResourceDto>,
    IRequestHandler<SetResourceParentCommand, Unit>,
    IRequestHandler<ActivateResourceCommand, Unit>,
    IRequestHandler<DeactivateResourceCommand, Unit>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<CreateResourceCommandHandler> _logger;

    public CreateResourceCommandHandler(
        IResourceRepository resourceRepository,
        ILogger<CreateResourceCommandHandler> logger)
    {
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResourceDto> Handle(CreateResourceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating resource with code {Code}", request.Code);

        // Check if code already exists
        if (await _resourceRepository.ExistsByCodeAsync(request.Code, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException($"Resource with code '{request.Code}' already exists");
        }

        // Validate parent resource exists if specified
        Resource? parentResource = null;
        if (request.ParentId.HasValue)
        {
            parentResource = await _resourceRepository.GetByIdAsync(request.ParentId.Value, cancellationToken).ConfigureAwait(false);
            if (parentResource == null)
            {
                throw new InvalidOperationException($"Parent resource with ID {request.ParentId.Value} not found");
            }
        }

        // Create the resource
        var resource = new Resource(
            request.Name,
            request.Code,
            request.Type,
            request.RequiresScheduling,
            request.Description);

        // Set parent hierarchy if specified
        if (parentResource != null)
        {
            resource.SetParent(parentResource.Id, parentResource.HierarchyPath);
        }

        await _resourceRepository.AddAsync(resource, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Created resource {ResourceId} with code {Code}", resource.Id, resource.Code);

        return MapToDto(resource);
    }

    public async Task<ResourceDto> Handle(UpdateResourceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating resource {ResourceId}", request.Id);

        var resource = await _resourceRepository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource with ID {request.Id} not found");
        }

        resource.UpdateResource(request.Name, request.RequiresScheduling, request.Description);
        await _resourceRepository.UpdateAsync(resource, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated resource {ResourceId}", resource.Id);

        return MapToDto(resource);
    }

    public async Task<Unit> Handle(SetResourceParentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Setting parent for resource {ResourceId} to {ParentId}",
            request.ResourceId, request.ParentId);

        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken).ConfigureAwait(false);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource with ID {request.ResourceId} not found");
        }

        if (request.ParentId.HasValue)
        {
            var parentResource = await _resourceRepository.GetByIdAsync(request.ParentId.Value, cancellationToken).ConfigureAwait(false);
            if (parentResource == null)
            {
                throw new InvalidOperationException($"Parent resource with ID {request.ParentId.Value} not found");
            }

            resource.SetParent(parentResource.Id, parentResource.HierarchyPath);
        }
        else
        {
            resource.RemoveParent();
        }

        await _resourceRepository.UpdateAsync(resource, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Set parent for resource {ResourceId}", resource.Id);

        return Unit.Value;
    }

    public async Task<Unit> Handle(ActivateResourceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Activating resource {ResourceId}", request.ResourceId);

        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken).ConfigureAwait(false);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource with ID {request.ResourceId} not found");
        }

        resource.Activate();
        await _resourceRepository.UpdateAsync(resource, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Activated resource {ResourceId}", resource.Id);

        return Unit.Value;
    }

    public async Task<Unit> Handle(DeactivateResourceCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating resource {ResourceId}", request.ResourceId);

        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken).ConfigureAwait(false);
        if (resource == null)
        {
            throw new InvalidOperationException($"Resource with ID {request.ResourceId} not found");
        }

        resource.Deactivate();
        await _resourceRepository.UpdateAsync(resource, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deactivated resource {ResourceId}", resource.Id);

        return Unit.Value;
    }

    private static ResourceDto MapToDto(Resource resource)
    {
        return new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Code = resource.Code,
            Type = resource.Type,
            ParentId = resource.ParentId,
            HierarchyPath = resource.HierarchyPath,
            RequiresScheduling = resource.RequiresScheduling,
            IsActive = resource.IsActive,
            Description = resource.Description,
            CreatedAt = resource.CreatedAt,
            UpdatedAt = resource.UpdatedAt
        };
    }
}
