using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Industrial.Adam.Security.Domain.Entities;

/// <summary>
/// CFR Part 11 compliant audit record with digital signature support
/// </summary>
public class AuditRecord
{
    public string Id { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string Username { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string Resource { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public DateTime Timestamp { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;
    public AuditSeverity Severity { get; private set; }
    public string? PreviousRecordHash { get; private set; }
    public string RecordHash { get; private set; } = string.Empty;
    public string? DigitalSignature { get; private set; }
    public AuditSignatureMetadata? SignatureMetadata { get; private set; }
    public bool IsModified { get; private set; }

    private AuditRecord() { } // EF constructor

    public AuditRecord(
        string id,
        string userId,
        string username,
        string action,
        string resource,
        string? details,
        string ipAddress,
        string userAgent,
        AuditSeverity severity = AuditSeverity.Information,
        Dictionary<string, object>? metadata = null,
        string? previousRecordHash = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        Details = details;
        IpAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
        UserAgent = userAgent ?? throw new ArgumentNullException(nameof(userAgent));
        Severity = severity;
        Timestamp = DateTime.UtcNow;
        Metadata = metadata ?? new Dictionary<string, object>();
        PreviousRecordHash = previousRecordHash;
        IsModified = false;

        // Generate record hash for integrity verification
        RecordHash = GenerateRecordHash();
    }

    /// <summary>
    /// Signs the audit record with a digital signature for CFR Part 11 compliance
    /// </summary>
    public void SignRecord(string signature, AuditSignatureMetadata signatureMetadata)
    {
        if (string.IsNullOrEmpty(signature))
            throw new ArgumentNullException(nameof(signature));
        
        if (signatureMetadata == null)
            throw new ArgumentNullException(nameof(signatureMetadata));

        DigitalSignature = signature;
        SignatureMetadata = signatureMetadata;
    }

    /// <summary>
    /// Verifies the integrity of the audit record
    /// </summary>
    public bool VerifyIntegrity()
    {
        var calculatedHash = GenerateRecordHash();
        return string.Equals(RecordHash, calculatedHash, StringComparison.Ordinal) && !IsModified;
    }

    /// <summary>
    /// Verifies the blockchain-style linking with the previous record
    /// </summary>
    public bool VerifyChainIntegrity(string? expectedPreviousHash)
    {
        if (PreviousRecordHash == null && expectedPreviousHash == null)
            return true; // First record in chain

        return string.Equals(PreviousRecordHash, expectedPreviousHash, StringComparison.Ordinal);
    }

    /// <summary>
    /// Adds metadata to the audit record
    /// </summary>
    public void AddMetadata(string key, object value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        Metadata[key] = value;
        
        // Regenerate hash after modification
        RecordHash = GenerateRecordHash();
    }

    /// <summary>
    /// Generates a SHA-256 hash of the record for integrity verification
    /// </summary>
    private string GenerateRecordHash()
    {
        var recordData = new
        {
            Id,
            UserId,
            Username,
            Action,
            Resource,
            Details,
            Timestamp = Timestamp.ToString("O"), // ISO 8601 format
            IpAddress,
            UserAgent,
            Severity = Severity.ToString(),
            Metadata = JsonSerializer.Serialize(Metadata, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            PreviousRecordHash
        };

        var json = JsonSerializer.Serialize(recordData, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

/// <summary>
/// Audit record severity levels
/// </summary>
public enum AuditSeverity
{
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4,
    SecurityEvent = 5
}

/// <summary>
/// Digital signature metadata for CFR Part 11 compliance
/// </summary>
public class AuditSignatureMetadata
{
    public string Algorithm { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    public string SignerCertificateThumbprint { get; set; } = string.Empty;
    public string SignerName { get; set; } = string.Empty;
    public string HashAlgorithm { get; set; } = string.Empty;
    public string KeySize { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();
}