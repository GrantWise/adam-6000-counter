using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IResourceRepository
/// </summary>
public sealed class ResourceRepository : IResourceRepository
{
    private readonly EquipmentSchedulingDbContext _context;
    private readonly ILogger<ResourceRepository> _logger;

    public ResourceRepository(
        EquipmentSchedulingDbContext context,
        ILogger<ResourceRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Resource?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting resource by ID {ResourceId}", id);

        return await _context.Resources
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Resource?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be null or empty", nameof(code));

        _logger.LogDebug("Getting resource by code {Code}", code);

        return await _context.Resources
            .FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant(), cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Resource>> GetByTypeAsync(ResourceType type, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting resources by type {Type}, activeOnly: {ActiveOnly}", type, activeOnly);

        var query = _context.Resources
            .Where(r => r.Type == type);

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Resource>> GetChildrenAsync(long parentId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting child resources for parent {ParentId}, activeOnly: {ActiveOnly}", parentId, activeOnly);

        var query = _context.Resources
            .Where(r => r.ParentId == parentId);

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Resource>> GetByHierarchyPathAsync(string hierarchyPath, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(hierarchyPath))
            throw new ArgumentException("Hierarchy path cannot be null or empty", nameof(hierarchyPath));

        _logger.LogDebug("Getting resources by hierarchy path {HierarchyPath}, activeOnly: {ActiveOnly}", hierarchyPath, activeOnly);

        var query = _context.Resources
            .Where(r => r.HierarchyPath != null && r.HierarchyPath.StartsWith(hierarchyPath));

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.HierarchyPath)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Resource>> GetSchedulableResourcesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting schedulable resources, activeOnly: {ActiveOnly}", activeOnly);

        var query = _context.Resources
            .Where(r => r.RequiresScheduling);

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        if (resource == null)
            throw new ArgumentNullException(nameof(resource));

        _logger.LogDebug("Adding resource {Code}", resource.Code);

        await _context.Resources.AddAsync(resource, cancellationToken).ConfigureAwait(false);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Added resource {ResourceId} with code {Code}", resource.Id, resource.Code);
    }

    public async Task UpdateAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        if (resource == null)
            throw new ArgumentNullException(nameof(resource));

        _logger.LogDebug("Updating resource {ResourceId}", resource.Id);

        _context.Resources.Update(resource);
        await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Updated resource {ResourceId}", resource.Id);
    }

    public async Task<bool> ExistsByCodeAsync(string code, long? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be null or empty", nameof(code));

        _logger.LogDebug("Checking if resource code {Code} exists, excludeId: {ExcludeId}", code, excludeId);

        var query = _context.Resources
            .Where(r => r.Code == code.ToUpperInvariant());

        if (excludeId.HasValue)
        {
            query = query.Where(r => r.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }
}
