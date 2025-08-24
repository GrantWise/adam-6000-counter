namespace Industrial.Adam.AdminDashboard.Domain.Entities;

/// <summary>
/// Base entity with 21 CFR Part 11 compliant audit trail fields
/// Implements immutable audit records for regulatory compliance
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    /// Gets or sets the user ID who created the record
    /// Immutable after initial creation for regulatory compliance
    /// </summary>
    public required string CreatedBy { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the record was created
    /// Immutable after initial creation for regulatory compliance
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the user ID who last modified the record
    /// Updated on each modification for audit trail
    /// </summary>
    public required string ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the record was last modified
    /// Updated on each modification for audit trail
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the digital signature for the record
    /// Used for 21 CFR Part 11 electronic signature requirements
    /// This would typically be a hash or cryptographic signature
    /// </summary>
    public string? DigitalSignature { get; set; }

    /// <summary>
    /// Gets or sets the version number of the record
    /// Incremented with each modification for change tracking
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the record has been archived/deleted
    /// Soft delete for audit trail preservation
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Gets or sets the user who deleted the record
    /// Required for audit trail when record is soft deleted
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets when the record was deleted
    /// Required for audit trail when record is soft deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the reason for modification/deletion
    /// Required for 21 CFR Part 11 audit trail explanations
    /// </summary>
    public string? ModificationReason { get; set; }
}