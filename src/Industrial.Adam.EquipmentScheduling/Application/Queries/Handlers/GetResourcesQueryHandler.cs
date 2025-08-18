using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries.Handlers;

/// <summary>
/// Handler for resource queries
/// </summary>
public sealed class GetResourcesQueryHandler :
    IRequestHandler<GetResourceByIdQuery, ResourceDto?>,
    IRequestHandler<GetResourceByCodeQuery, ResourceDto?>,
    IRequestHandler<GetResourcesByTypeQuery, IEnumerable<ResourceDto>>,
    IRequestHandler<GetChildResourcesQuery, IEnumerable<ResourceDto>>,
    IRequestHandler<GetResourceHierarchyQuery, IEnumerable<ResourceHierarchyDto>>,
    IRequestHandler<GetSchedulableResourcesQuery, IEnumerable<ResourceDto>>,
    IRequestHandler<GetResourcesByHierarchyPathQuery, IEnumerable<ResourceDto>>
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ILogger<GetResourcesQueryHandler> _logger;

    public GetResourcesQueryHandler(
        IResourceRepository resourceRepository,
        ILogger<GetResourcesQueryHandler> logger)
    {
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResourceDto?> Handle(GetResourceByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resource by ID {ResourceId}", request.ResourceId);

        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        return resource == null ? null : MapToDto(resource);
    }

    public async Task<ResourceDto?> Handle(GetResourceByCodeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resource by code {Code}", request.Code);

        var resource = await _resourceRepository.GetByCodeAsync(request.Code, cancellationToken);
        return resource == null ? null : MapToDto(resource);
    }

    public async Task<IEnumerable<ResourceDto>> Handle(GetResourcesByTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resources by type {Type}, activeOnly: {ActiveOnly}", request.Type, request.ActiveOnly);

        var resources = await _resourceRepository.GetByTypeAsync(request.Type, request.ActiveOnly, cancellationToken);
        return resources.Select(MapToDto);
    }

    public async Task<IEnumerable<ResourceDto>> Handle(GetChildResourcesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting child resources for parent {ParentId}, activeOnly: {ActiveOnly}",
            request.ParentId, request.ActiveOnly);

        var resources = await _resourceRepository.GetChildrenAsync(request.ParentId, request.ActiveOnly, cancellationToken);
        return resources.Select(MapToDto);
    }

    public async Task<IEnumerable<ResourceHierarchyDto>> Handle(GetResourceHierarchyQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resource hierarchy, root: {RootResourceId}, activeOnly: {ActiveOnly}",
            request.RootResourceId, request.ActiveOnly);

        // This is a simplified implementation - in a real scenario, you'd want to optimize this
        // to build the hierarchy in a single query or use a cached approach

        var allResources = request.RootResourceId.HasValue
            ? await _resourceRepository.GetChildrenAsync(request.RootResourceId.Value, request.ActiveOnly, cancellationToken)
            : await _resourceRepository.GetByTypeAsync(Domain.Enums.ResourceType.Enterprise, request.ActiveOnly, cancellationToken);

        var resourceList = allResources.ToList();
        var hierarchyList = new List<ResourceHierarchyDto>();

        foreach (var resource in resourceList.Where(r => r.ParentId == request.RootResourceId))
        {
            var hierarchyDto = await BuildHierarchyDto(resource, resourceList, cancellationToken);
            hierarchyList.Add(hierarchyDto);
        }

        return hierarchyList;
    }

    public async Task<IEnumerable<ResourceDto>> Handle(GetSchedulableResourcesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting schedulable resources, activeOnly: {ActiveOnly}", request.ActiveOnly);

        var resources = await _resourceRepository.GetSchedulableResourcesAsync(request.ActiveOnly, cancellationToken);
        return resources.Select(MapToDto);
    }

    public async Task<IEnumerable<ResourceDto>> Handle(GetResourcesByHierarchyPathQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting resources by hierarchy path {HierarchyPath}, activeOnly: {ActiveOnly}",
            request.HierarchyPath, request.ActiveOnly);

        var resources = await _resourceRepository.GetByHierarchyPathAsync(request.HierarchyPath, request.ActiveOnly, cancellationToken);
        return resources.Select(MapToDto);
    }

    private async Task<ResourceHierarchyDto> BuildHierarchyDto(
        Domain.Entities.Resource resource,
        IReadOnlyList<Domain.Entities.Resource> allResources,
        CancellationToken cancellationToken)
    {
        var dto = new ResourceHierarchyDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Code = resource.Code,
            Type = resource.Type,
            ParentId = resource.ParentId,
            Level = CalculateLevel(resource.HierarchyPath),
            Children = new List<ResourceHierarchyDto>()
        };

        // Get children from the list
        var children = allResources.Where(r => r.ParentId == resource.Id).ToList();

        foreach (var child in children)
        {
            var childDto = await BuildHierarchyDto(child, allResources, cancellationToken);
            dto.Children.Add(childDto);
        }

        return dto;
    }

    private static int CalculateLevel(string? hierarchyPath)
    {
        if (string.IsNullOrEmpty(hierarchyPath))
            return 0;

        // Count the number of slashes to determine level
        return hierarchyPath.Count(c => c == '/') - 1;
    }

    private static ResourceDto MapToDto(Domain.Entities.Resource resource)
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
