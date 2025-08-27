using Industrial.Adam.Security.Domain.Entities;
using MediatR;

namespace Industrial.Adam.Security.Application.Commands.DigitalSignature;

/// <summary>
/// Command to sign an audit record with digital signature for CFR Part 11 compliance
/// </summary>
public class SignAuditRecordCommand : IRequest<SignAuditRecordResponse>
{
    public string AuditRecordId { get; set; } = string.Empty;
    public string UserCertificate { get; set; } = string.Empty;
    public string SigningUserId { get; set; } = string.Empty;
    public string SigningUsername { get; set; } = string.Empty;
    public string SigningReason { get; set; } = string.Empty;
    public Dictionary<string, string> AdditionalMetadata { get; set; } = new();
}

/// <summary>
/// Response for audit record signing
/// </summary>
public class SignAuditRecordResponse
{
    public bool Success { get; set; }
    public string? DigitalSignature { get; set; }
    public AuditSignatureMetadata? SignatureMetadata { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SignedAt { get; set; }

    public static SignAuditRecordResponse Failure(string error)
    {
        return new SignAuditRecordResponse
        {
            Success = false,
            Errors = new List<string> { error }
        };
    }

    public static SignAuditRecordResponse Failure(IEnumerable<string> errors)
    {
        return new SignAuditRecordResponse
        {
            Success = false,
            Errors = errors.ToList()
        };
    }
}