using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Domain.Interfaces;

/// <summary>
/// Repository interface for resource operations
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Gets a resource by its identifier
    /// </summary>
    /// <param name="id">The resource identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resource, or null if not found</returns>
    public Task<Resource?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a resource by its code
    /// </summary>
    /// <param name="code">The resource code</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The resource, or null if not found</returns>
    public Task<Resource?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources of a specific type
    /// </summary>
    /// <param name="type">The resource type</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources</returns>
    public Task<IEnumerable<Resource>> GetByTypeAsync(ResourceType type, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets child resources of a parent resource
    /// </summary>
    /// <param name="parentId">The parent resource identifier</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of child resources</returns>
    public Task<IEnumerable<Resource>> GetChildrenAsync(long parentId, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources in a hierarchy path
    /// </summary>
    /// <param name="hierarchyPath">The hierarchy path to search</param>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources in the hierarchy</returns>
    public Task<IEnumerable<Resource>> GetByHierarchyPathAsync(string hierarchyPath, bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets resources that require scheduling
    /// </summary>
    /// <param name="activeOnly">Whether to include only active resources</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of resources requiring scheduling</returns>
    public Task<IEnumerable<Resource>> GetSchedulableResourcesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new resource
    /// </summary>
    /// <param name="resource">The resource to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task AddAsync(Resource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing resource
    /// </summary>
    /// <param name="resource">The resource to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public Task UpdateAsync(Resource resource, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a resource code already exists
    /// </summary>
    /// <param name="code">The resource code to check</param>
    /// <param name="excludeId">Resource ID to exclude from the check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the code exists</returns>
    public Task<bool> ExistsByCodeAsync(string code, long? excludeId = null, CancellationToken cancellationToken = default);
}
