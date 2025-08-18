using System.ComponentModel.DataAnnotations;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

namespace Industrial.Adam.EquipmentScheduling.Domain.Entities;

/// <summary>
/// Represents equipment resources in an ISA-95 compliant hierarchy
/// </summary>
public sealed class Resource : Entity<long>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the name of the resource
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the unique code identifying this resource
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the ISA-95 resource type
    /// </summary>
    public ResourceType Type { get; private set; }

    /// <summary>
    /// Gets the parent resource identifier (null for top-level resources)
    /// </summary>
    public long? ParentId { get; private set; }

    /// <summary>
    /// Gets the hierarchical path for efficient querying (e.g., "/1/5/12/")
    /// </summary>
    [StringLength(500)]
    public string? HierarchyPath { get; private set; }

    /// <summary>
    /// Gets whether this resource requires scheduling
    /// </summary>
    public bool RequiresScheduling { get; private set; }

    /// <summary>
    /// Gets whether this resource is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Gets optional description of the resource
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; private set; }

    /// <summary>
    /// Navigation property for child resources
    /// </summary>
    public ICollection<Resource> Children { get; private set; } = [];

    /// <summary>
    /// Navigation property for pattern assignments
    /// </summary>
    public ICollection<PatternAssignment> PatternAssignments { get; private set; } = [];

    /// <summary>
    /// Gets the domain events for this aggregate root
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core
    private Resource() : base() { }

    /// <summary>
    /// Creates a new resource
    /// </summary>
    /// <param name="name">The resource name</param>
    /// <param name="code">The unique resource code</param>
    /// <param name="type">The ISA-95 resource type</param>
    /// <param name="requiresScheduling">Whether this resource requires scheduling</param>
    /// <param name="description">Optional description</param>
    public Resource(
        string name,
        string code,
        ResourceType type,
        bool requiresScheduling = false,
        string? description = null) : base()
    {
        ValidateResourceCreation(name, code, type);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        Type = type;
        RequiresScheduling = requiresScheduling;
        Description = description?.Trim();

        AddDomainEvent(new ResourceCreatedEvent(Id, Name, Code, Type));
    }

    /// <summary>
    /// Updates the resource properties
    /// </summary>
    /// <param name="name">The new name</param>
    /// <param name="requiresScheduling">Whether scheduling is required</param>
    /// <param name="description">Optional description</param>
    public void UpdateResource(string name, bool requiresScheduling, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resource name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Resource name cannot exceed 200 characters", nameof(name));

        var oldName = Name;
        var oldRequiresScheduling = RequiresScheduling;

        Name = name.Trim();
        RequiresScheduling = requiresScheduling;
        Description = description?.Trim();

        MarkAsUpdated();

        if (oldName != Name || oldRequiresScheduling != RequiresScheduling)
        {
            AddDomainEvent(new ResourceUpdatedEvent(Id, Name, RequiresScheduling));
        }
    }

    /// <summary>
    /// Sets the parent resource and updates hierarchy path
    /// </summary>
    /// <param name="parentId">The parent resource identifier</param>
    /// <param name="parentHierarchyPath">The parent's hierarchy path</param>
    public void SetParent(long parentId, string? parentHierarchyPath)
    {
        if (parentId == Id)
            throw new InvalidOperationException("Resource cannot be its own parent");

        ParentId = parentId;
        HierarchyPath = string.IsNullOrEmpty(parentHierarchyPath)
            ? $"/{parentId}/{Id}/"
            : $"{parentHierarchyPath.TrimEnd('/')}/{Id}/";

        MarkAsUpdated();
        AddDomainEvent(new ResourceHierarchyChangedEvent(Id, ParentId, HierarchyPath));
    }

    /// <summary>
    /// Removes the parent relationship (makes this a root resource)
    /// </summary>
    public void RemoveParent()
    {
        ParentId = null;
        HierarchyPath = $"/{Id}/";

        MarkAsUpdated();
        AddDomainEvent(new ResourceHierarchyChangedEvent(Id, null, HierarchyPath));
    }

    /// <summary>
    /// Deactivates the resource
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        MarkAsUpdated();
        AddDomainEvent(new ResourceDeactivatedEvent(Id, Name));
    }

    /// <summary>
    /// Reactivates the resource
    /// </summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        MarkAsUpdated();
        AddDomainEvent(new ResourceActivatedEvent(Id, Name));
    }

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <summary>
    /// Gets all descendant resources based on hierarchy path
    /// </summary>
    /// <returns>True if the given resource is a descendant</returns>
    public bool IsAncestorOf(Resource other)
    {
        return other.HierarchyPath?.StartsWith(HierarchyPath ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void ValidateResourceCreation(string name, string code, ResourceType type)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Resource name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Resource name cannot exceed 200 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Resource code cannot be empty", nameof(code));

        if (code.Length > 50)
            throw new ArgumentException("Resource code cannot exceed 50 characters", nameof(code));

        if (!Enum.IsDefined(typeof(ResourceType), type))
            throw new ArgumentException("Invalid resource type", nameof(type));
    }
}
