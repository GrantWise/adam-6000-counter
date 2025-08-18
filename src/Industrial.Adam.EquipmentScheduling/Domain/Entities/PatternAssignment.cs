using System.ComponentModel.DataAnnotations;
using Industrial.Adam.EquipmentScheduling.Domain.Events;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Domain.ValueObjects;

namespace Industrial.Adam.EquipmentScheduling.Domain.Entities;

/// <summary>
/// Represents the assignment of an operating pattern to a resource
/// </summary>
public sealed class PatternAssignment : Entity<long>, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the resource identifier
    /// </summary>
    public long ResourceId { get; private set; }

    /// <summary>
    /// Gets the pattern identifier
    /// </summary>
    public int PatternId { get; private set; }

    /// <summary>
    /// Gets the effective date when this assignment becomes active
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Gets the end date when this assignment expires (null for indefinite)
    /// </summary>
    public DateTime? EndDate { get; private set; }

    /// <summary>
    /// Gets whether this is a temporary override assignment
    /// </summary>
    public bool IsOverride { get; private set; }

    /// <summary>
    /// Gets who assigned this pattern
    /// </summary>
    [StringLength(100)]
    public string? AssignedBy { get; private set; }

    /// <summary>
    /// Gets when this assignment was created
    /// </summary>
    public DateTime AssignedAt { get; private set; }

    /// <summary>
    /// Gets optional notes about this assignment
    /// </summary>
    [StringLength(500)]
    public string? Notes { get; private set; }

    /// <summary>
    /// Navigation property to the resource
    /// </summary>
    public Resource Resource { get; private set; } = null!;

    /// <summary>
    /// Navigation property to the operating pattern
    /// </summary>
    public OperatingPattern OperatingPattern { get; private set; } = null!;

    /// <summary>
    /// Gets the domain events for this aggregate root
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Required by EF Core
    private PatternAssignment() : base() { }

    /// <summary>
    /// Creates a new pattern assignment
    /// </summary>
    /// <param name="resourceId">The resource identifier</param>
    /// <param name="patternId">The pattern identifier</param>
    /// <param name="effectiveDate">The effective date</param>
    /// <param name="endDate">Optional end date</param>
    /// <param name="isOverride">Whether this is an override assignment</param>
    /// <param name="assignedBy">Who assigned this pattern</param>
    /// <param name="notes">Optional notes</param>
    public PatternAssignment(
        long resourceId,
        int patternId,
        DateTime effectiveDate,
        DateTime? endDate = null,
        bool isOverride = false,
        string? assignedBy = null,
        string? notes = null) : base()
    {
        ValidateAssignmentCreation(resourceId, patternId, effectiveDate, endDate);

        ResourceId = resourceId;
        PatternId = patternId;
        EffectiveDate = effectiveDate.Date; // Normalize to date only
        EndDate = endDate?.Date; // Normalize to date only
        IsOverride = isOverride;
        AssignedBy = assignedBy?.Trim();
        AssignedAt = DateTime.UtcNow;
        Notes = notes?.Trim();

        AddDomainEvent(new PatternAssignmentCreatedEvent(Id, ResourceId, PatternId, EffectiveDate, IsOverride));
    }

    /// <summary>
    /// Updates the assignment end date
    /// </summary>
    /// <param name="endDate">The new end date</param>
    public void UpdateEndDate(DateTime? endDate)
    {
        if (endDate.HasValue && endDate.Value.Date < EffectiveDate.Date)
            throw new ArgumentException("End date cannot be before effective date", nameof(endDate));

        var oldEndDate = EndDate;
        EndDate = endDate?.Date;

        MarkAsUpdated();

        if (oldEndDate != EndDate)
        {
            AddDomainEvent(new PatternAssignmentUpdatedEvent(Id, ResourceId, PatternId, EndDate));
        }
    }

    /// <summary>
    /// Updates the assignment notes
    /// </summary>
    /// <param name="notes">The new notes</param>
    /// <param name="updatedBy">Who updated the assignment</param>
    public void UpdateNotes(string? notes, string? updatedBy = null)
    {
        if (notes?.Length > 500)
            throw new ArgumentException("Notes cannot exceed 500 characters", nameof(notes));

        Notes = notes?.Trim();
        AssignedBy = updatedBy?.Trim() ?? AssignedBy;

        MarkAsUpdated();
    }

    /// <summary>
    /// Terminates the assignment by setting the end date to today
    /// </summary>
    /// <param name="terminatedBy">Who terminated the assignment</param>
    public void Terminate(string? terminatedBy = null)
    {
        var today = DateTime.UtcNow.Date;

        if (EndDate.HasValue && EndDate.Value <= today)
            return; // Already terminated

        EndDate = today;
        AssignedBy = terminatedBy?.Trim() ?? AssignedBy;

        MarkAsUpdated();
        AddDomainEvent(new PatternAssignmentTerminatedEvent(Id, ResourceId, PatternId, today));
    }

    /// <summary>
    /// Checks if this assignment is active on the specified date
    /// </summary>
    /// <param name="date">The date to check</param>
    /// <returns>True if the assignment is active on the specified date</returns>
    public bool IsActiveOn(DateTime date)
    {
        var checkDate = date.Date;

        if (checkDate < EffectiveDate.Date)
            return false;

        if (EndDate.HasValue && checkDate > EndDate.Value.Date)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if this assignment overlaps with another assignment
    /// </summary>
    /// <param name="other">The other assignment to check</param>
    /// <returns>True if the assignments overlap</returns>
    public bool OverlapsWith(PatternAssignment other)
    {
        if (ResourceId != other.ResourceId)
            return false;

        var thisStart = EffectiveDate.Date;
        var thisEnd = EndDate?.Date ?? DateTime.MaxValue.Date;
        var otherStart = other.EffectiveDate.Date;
        var otherEnd = other.EndDate?.Date ?? DateTime.MaxValue.Date;

        return thisStart <= otherEnd && thisEnd >= otherStart;
    }

    /// <summary>
    /// Gets the duration of this assignment in days
    /// </summary>
    /// <returns>The duration in days, or null for indefinite assignments</returns>
    public int? GetDurationDays()
    {
        if (!EndDate.HasValue)
            return null;

        return (int)(EndDate.Value.Date - EffectiveDate.Date).TotalDays + 1;
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

    private static void ValidateAssignmentCreation(long resourceId, int patternId, DateTime effectiveDate, DateTime? endDate)
    {
        if (resourceId <= 0)
            throw new ArgumentException("Resource ID must be positive", nameof(resourceId));

        if (patternId <= 0)
            throw new ArgumentException("Pattern ID must be positive", nameof(patternId));

        if (effectiveDate == default)
            throw new ArgumentException("Effective date must be specified", nameof(effectiveDate));

        if (endDate.HasValue && endDate.Value.Date < effectiveDate.Date)
            throw new ArgumentException("End date cannot be before effective date", nameof(endDate));
    }
}
