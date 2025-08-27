using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Domain.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

namespace Industrial.Adam.Security.Infrastructure.Services;

/// <summary>
/// Digital signature service implementation for CFR Part 11 compliance
/// </summary>
public class DigitalSignatureService : IDigitalSignatureService
{
    private readonly ILogger<DigitalSignatureService> _logger;
    private const string SigningAlgorithm = "SHA256withRSA";
    private const string HashAlgorithm = "SHA256";

    public DigitalSignatureService(ILogger<DigitalSignatureService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> SignAuditRecordAsync(AuditRecord record, string userCertificate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Signing audit record {AuditRecordId}", record.Id);

            // Validate inputs
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            
            if (string.IsNullOrWhiteSpace(userCertificate))
                throw new ArgumentNullException(nameof(userCertificate));

            // Parse certificate
            var certificate = ParseCertificate(userCertificate);
            if (certificate == null)
                throw new InvalidOperationException("Failed to parse user certificate");

            // Create signable data from audit record
            var signableData = CreateSignableData(record);
            var dataBytes = Encoding.UTF8.GetBytes(signableData);

            // Sign the data
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa == null)
                throw new InvalidOperationException("Certificate does not contain RSA private key");

            var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var signature = Convert.ToBase64String(signatureBytes);

            _logger.LogInformation("Successfully signed audit record {AuditRecordId}", record.Id);

            return await Task.FromResult(signature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing audit record {AuditRecordId}", record?.Id);
            throw;
        }
    }

    public async Task<bool> VerifySignatureAsync(AuditRecord record, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Verifying signature for audit record {AuditRecordId}", record.Id);

            // Validate inputs
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            
            if (string.IsNullOrWhiteSpace(signature))
                throw new ArgumentNullException(nameof(signature));

            if (record.SignatureMetadata == null)
                throw new InvalidOperationException("Signature metadata is required for verification");

            // Get certificate from signature metadata
            var certificate = await GetCertificateFromThumbprintAsync(record.SignatureMetadata.SignerCertificateThumbprint, cancellationToken);
            if (certificate == null)
            {
                _logger.LogWarning("Certificate not found for thumbprint {Thumbprint}", record.SignatureMetadata.SignerCertificateThumbprint);
                return false;
            }

            // Create signable data from audit record (same as signing process)
            var signableData = CreateSignableData(record);
            var dataBytes = Encoding.UTF8.GetBytes(signableData);
            var signatureBytes = Convert.FromBase64String(signature);

            // Verify signature
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa == null)
            {
                _logger.LogWarning("Certificate does not contain RSA public key");
                return false;
            }

            var isValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            _logger.LogInformation("Signature verification for audit record {AuditRecordId}: {IsValid}", record.Id, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying signature for audit record {AuditRecordId}", record?.Id);
            return false;
        }
    }

    public async Task<AuditSignatureMetadata> GetSignatureMetadataAsync(string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            // In a production environment, this would extract metadata from the signature
            // For this implementation, we'll create basic metadata
            return await Task.FromResult(new AuditSignatureMetadata
            {
                Algorithm = SigningAlgorithm,
                SignedAt = DateTime.UtcNow,
                SignerCertificateThumbprint = "placeholder-thumbprint", // Would be extracted from actual certificate
                SignerName = "System User", // Would be extracted from certificate subject
                HashAlgorithm = HashAlgorithm,
                KeySize = "2048", // Standard RSA key size
                AdditionalMetadata = new Dictionary<string, string>
                {
                    ["SignatureLength"] = signature.Length.ToString(),
                    ["SigningSystem"] = "Industrial.Adam.Security",
                    ["ComplianceStandard"] = "CFR Part 11"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting signature metadata");
            throw;
        }
    }

    public async Task<bool> ValidateCertificateAsync(string certificate, CancellationToken cancellationToken = default)
    {
        try
        {
            var cert = ParseCertificate(certificate);
            if (cert == null)
                return false;

            // Check certificate validity period
            if (DateTime.UtcNow < cert.NotBefore || DateTime.UtcNow > cert.NotAfter)
            {
                _logger.LogWarning("Certificate is outside validity period: {NotBefore} to {NotAfter}", cert.NotBefore, cert.NotAfter);
                return false;
            }

            // Check if certificate has private key for signing
            if (!cert.HasPrivateKey)
            {
                _logger.LogWarning("Certificate does not have a private key");
                return false;
            }

            // Additional validation could include:
            // - Certificate chain validation
            // - Certificate revocation checking
            // - Key usage validation
            // - Certificate authority validation

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating certificate");
            return false;
        }
    }

    public async Task<SigningCertificate> GenerateCertificateAsync(string userId, string username, TimeSpan validityPeriod, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating new certificate for user {UserId}", userId);

            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest($"CN={username}, OU=Industrial Users, O=Industrial.Adam.System", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Add key usage extensions for digital signatures
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));

            // Add enhanced key usage for code signing (appropriate for audit record signing)
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new("1.3.6.1.5.5.7.3.3") }, true));

            // Generate self-signed certificate (in production, this would be signed by a CA)
            var certificate = request.CreateSelfSigned(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.Add(validityPeriod));

            // Export certificate and private key
            var certBytes = certificate.Export(X509ContentType.Pfx, "temp-password");
            var certData = Convert.ToBase64String(certBytes);
            
            var privateKeyBytes = rsa.ExportRSAPrivateKey();
            var privateKeyData = Convert.ToBase64String(privateKeyBytes);

            var result = new SigningCertificate
            {
                CertificateData = certData,
                PrivateKey = privateKeyData,
                Thumbprint = certificate.Thumbprint,
                ValidFrom = certificate.NotBefore,
                ValidUntil = certificate.NotAfter,
                Subject = certificate.Subject,
                Issuer = certificate.Issuer
            };

            _logger.LogInformation("Successfully generated certificate for user {UserId} with thumbprint {Thumbprint}", 
                userId, certificate.Thumbprint);

            return await Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating certificate for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Creates signable data representation of audit record
    /// </summary>
    private static string CreateSignableData(AuditRecord record)
    {
        // Create a consistent, ordered representation of the audit record for signing
        var signableData = new
        {
            Id = record.Id,
            UserId = record.UserId,
            Username = record.Username,
            Action = record.Action,
            Resource = record.Resource,
            Details = record.Details ?? "",
            Timestamp = record.Timestamp.ToString("O"), // ISO 8601 format
            IpAddress = record.IpAddress,
            UserAgent = record.UserAgent,
            Severity = record.Severity.ToString(),
            Metadata = JsonSerializer.Serialize(record.Metadata, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            RecordHash = record.RecordHash,
            PreviousRecordHash = record.PreviousRecordHash ?? ""
        };

        return JsonSerializer.Serialize(signableData, new JsonSerializerOptions 
        { 
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Parses certificate from various formats (PEM, Base64, etc.)
    /// </summary>
    private X509Certificate2? ParseCertificate(string certificateData)
    {
        try
        {
            // Try different certificate formats
            if (certificateData.StartsWith("-----BEGIN CERTIFICATE-----"))
            {
                // PEM format
                return X509CertificateLoader.LoadCertificate(Encoding.UTF8.GetBytes(certificateData));
            }
            else
            {
                // Base64 format
                var certBytes = Convert.FromBase64String(certificateData);
                return X509CertificateLoader.LoadCertificate(certBytes);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse certificate");
            return null;
        }
    }

    /// <summary>
    /// Gets certificate by thumbprint from certificate store or database
    /// </summary>
    private async Task<X509Certificate2?> GetCertificateFromThumbprintAsync(string thumbprint, CancellationToken cancellationToken)
    {
        try
        {
            // In production, this would query a certificate store or database
            // For this implementation, we'll return a placeholder
            _logger.LogWarning("Certificate lookup by thumbprint not implemented: {Thumbprint}", thumbprint);
            return await Task.FromResult<X509Certificate2?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving certificate by thumbprint {Thumbprint}", thumbprint);
            return null;
        }
    }
}