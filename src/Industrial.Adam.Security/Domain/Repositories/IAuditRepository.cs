using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Domain.Repositories;

/// <summary>
/// Audit repository interface for CFR Part 11 compliant audit trail
/// </summary>
public interface IAuditRepository
{
    /// <summary>
    /// Creates a new audit record
    /// </summary>
    Task<string> CreateAsync(AuditRecord record, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit records with pagination and filtering
    /// </summary>
    Task<(List<AuditRecord> Records, int TotalCount)> GetAuditRecordsAsync(
        AuditSearchCriteria criteria,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit record by ID
    /// </summary>
    Task<AuditRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit records for a specific user
    /// </summary>
    Task<List<AuditRecord>> GetByUserIdAsync(string userId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit records for a specific action
    /// </summary>
    Task<List<AuditRecord>> GetByActionAsync(string action, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit records by severity level
    /// </summary>
    Task<List<AuditRecord>> GetBySeverityAsync(AuditSeverity severity, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last audit record hash for blockchain-style linking
    /// </summary>
    Task<string?> GetLastRecordHashAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the integrity of the entire audit chain
    /// </summary>
    Task<AuditChainIntegrityResult> VerifyAuditChainIntegrityAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets audit statistics for reporting
    /// </summary>
    Task<AuditStatistics> GetAuditStatisticsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old audit records (for compliance retention)
    /// </summary>
    Task<int> ArchiveOldRecordsAsync(DateTime archiveBefore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports audit records for compliance reporting
    /// </summary>
    Task<byte[]> ExportAuditRecordsAsync(AuditExportCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets security events that may indicate threats
    /// </summary>
    Task<List<AuditRecord>> GetSecurityEventsAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit search criteria
/// </summary>
public class AuditSearchCriteria
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? Action { get; set; }
    public string? Resource { get; set; }
    public AuditSeverity? Severity { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? IpAddress { get; set; }
    public string? SearchText { get; set; }
    public bool? HasDigitalSignature { get; set; }
    public bool? IntegrityVerified { get; set; }
}

/// <summary>
/// Audit chain integrity verification result
/// </summary>
public class AuditChainIntegrityResult
{
    public bool IsValid { get; set; }
    public int TotalRecords { get; set; }
    public int ValidRecords { get; set; }
    public int InvalidRecords { get; set; }
    public List<string> BrokenChainPoints { get; set; } = new();
    public List<string> IntegrityViolations { get; set; } = new();
    public DateTime VerificationTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Audit statistics for reporting
/// </summary>
public class AuditStatistics
{
    public int TotalRecords { get; set; }
    public Dictionary<string, int> ActionCounts { get; set; } = new();
    public Dictionary<string, int> UserCounts { get; set; } = new();
    public Dictionary<AuditSeverity, int> SeverityCounts { get; set; } = new();
    public Dictionary<DateTime, int> DailyCounts { get; set; } = new();
    public int SignedRecords { get; set; }
    public int UnsignedRecords { get; set; }
    public List<string> TopUsers { get; set; } = new();
    public List<string> TopActions { get; set; } = new();
}

/// <summary>
/// Audit export criteria
/// </summary>
public class AuditExportCriteria
{
    public AuditSearchCriteria SearchCriteria { get; set; } = new();
    public AuditExportFormat Format { get; set; } = AuditExportFormat.Csv;
    public bool IncludeDigitalSignatures { get; set; } = true;
    public bool VerifyIntegrityBeforeExport { get; set; } = true;
    public string? ExportedBy { get; set; }
    public string? ExportReason { get; set; }
}

/// <summary>
/// Audit export formats
/// </summary>
public enum AuditExportFormat
{
    Csv = 1,
    Json = 2,
    Xml = 3,
    Pdf = 4
}