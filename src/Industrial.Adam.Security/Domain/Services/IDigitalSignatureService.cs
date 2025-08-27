using Industrial.Adam.Security.Domain.Entities;

namespace Industrial.Adam.Security.Domain.Services;

/// <summary>
/// Digital signature service interface for CFR Part 11 compliance
/// </summary>
public interface IDigitalSignatureService
{
    /// <summary>
    /// Signs an audit record with a digital signature
    /// </summary>
    /// <param name="record">The audit record to sign</param>
    /// <param name="userCertificate">The user's certificate for signing</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The digital signature</returns>
    Task<string> SignAuditRecordAsync(AuditRecord record, string userCertificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a digital signature on an audit record
    /// </summary>
    /// <param name="record">The audit record with signature</param>
    /// <param name="signature">The signature to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if signature is valid</returns>
    Task<bool> VerifySignatureAsync(AuditRecord record, string signature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets signature metadata for an audit record
    /// </summary>
    /// <param name="signature">The signature to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signature metadata</returns>
    Task<AuditSignatureMetadata> GetSignatureMetadataAsync(string signature, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates certificate for signing operations
    /// </summary>
    /// <param name="certificate">The certificate to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if certificate is valid for signing</returns>
    Task<bool> ValidateCertificateAsync(string certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a new signing certificate for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="username">Username</param>
    /// <param name="validityPeriod">Certificate validity period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated certificate data</returns>
    Task<SigningCertificate> GenerateCertificateAsync(string userId, string username, TimeSpan validityPeriod, CancellationToken cancellationToken = default);
}

/// <summary>
/// Signing certificate data
/// </summary>
public class SigningCertificate
{
    public string CertificateData { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
    public string Thumbprint { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
}