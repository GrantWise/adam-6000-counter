using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using MediatR;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries;

/// <summary>
/// Query to get a resource by ID
/// </summary>
public sealed record GetResourceByIdQuery(
    long ResourceId) : IRequest<ResourceDto?>;

/// <summary>
/// Query to get a resource by code
/// </summary>
public sealed record GetResourceByCodeQuery(
    string Code) : IRequest<ResourceDto?>;

/// <summary>
/// Query to get resources by type
/// </summary>
public sealed record GetResourcesByTypeQuery(
    ResourceType Type,
    bool ActiveOnly = true) : IRequest<IEnumerable<ResourceDto>>;

/// <summary>
/// Query to get child resources
/// </summary>
public sealed record GetChildResourcesQuery(
    long ParentId,
    bool ActiveOnly = true) : IRequest<IEnumerable<ResourceDto>>;

/// <summary>
/// Query to get resource hierarchy
/// </summary>
public sealed record GetResourceHierarchyQuery(
    long? RootResourceId = null,
    bool ActiveOnly = true) : IRequest<IEnumerable<ResourceHierarchyDto>>;

/// <summary>
/// Query to get schedulable resources
/// </summary>
public sealed record GetSchedulableResourcesQuery(
    bool ActiveOnly = true) : IRequest<IEnumerable<ResourceDto>>;

/// <summary>
/// Query to get resources by hierarchy path
/// </summary>
public sealed record GetResourcesByHierarchyPathQuery(
    string HierarchyPath,
    bool ActiveOnly = true) : IRequest<IEnumerable<ResourceDto>>;
