using Industrial.Adam.Security.Domain.Repositories;
using Industrial.Adam.Security.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Application.Commands.DigitalSignature;

/// <summary>
/// Handler for signing audit records with digital signatures
/// </summary>
public class SignAuditRecordCommandHandler : IRequestHandler<SignAuditRecordCommand, SignAuditRecordResponse>
{
    private readonly IAuditRepository _auditRepository;
    private readonly IDigitalSignatureService _digitalSignatureService;
    private readonly ILogger<SignAuditRecordCommandHandler> _logger;

    public SignAuditRecordCommandHandler(
        IAuditRepository auditRepository,
        IDigitalSignatureService digitalSignatureService,
        ILogger<SignAuditRecordCommandHandler> logger)
    {
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _digitalSignatureService = digitalSignatureService ?? throw new ArgumentNullException(nameof(digitalSignatureService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SignAuditRecordResponse> Handle(SignAuditRecordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Signing audit record {AuditRecordId} by user {SigningUserId}", 
                request.AuditRecordId, request.SigningUserId);

            // Validate input
            var validationErrors = ValidateRequest(request);
            if (validationErrors.Any())
            {
                _logger.LogWarning("Validation failed for audit record signing: {Errors}", string.Join(", ", validationErrors));
                return SignAuditRecordResponse.Failure(validationErrors);
            }

            // Get the audit record
            var auditRecord = await _auditRepository.GetByIdAsync(request.AuditRecordId, cancellationToken);
            if (auditRecord == null)
            {
                _logger.LogWarning("Audit record {AuditRecordId} not found", request.AuditRecordId);
                return SignAuditRecordResponse.Failure("Audit record not found");
            }

            // Check if already signed
            if (!string.IsNullOrEmpty(auditRecord.DigitalSignature))
            {
                _logger.LogWarning("Audit record {AuditRecordId} is already digitally signed", request.AuditRecordId);
                return SignAuditRecordResponse.Failure("Audit record is already digitally signed");
            }

            // Validate certificate
            var isCertificateValid = await _digitalSignatureService.ValidateCertificateAsync(request.UserCertificate, cancellationToken);
            if (!isCertificateValid)
            {
                _logger.LogWarning("Invalid certificate provided for signing audit record {AuditRecordId}", request.AuditRecordId);
                return SignAuditRecordResponse.Failure("Invalid certificate provided");
            }

            // Verify audit record integrity before signing
            if (!auditRecord.VerifyIntegrity())
            {
                _logger.LogError("Audit record {AuditRecordId} failed integrity check before signing", request.AuditRecordId);
                return SignAuditRecordResponse.Failure("Audit record failed integrity verification");
            }

            // Generate digital signature
            var signature = await _digitalSignatureService.SignAuditRecordAsync(auditRecord, request.UserCertificate, cancellationToken);
            
            // Get signature metadata
            var signatureMetadata = await _digitalSignatureService.GetSignatureMetadataAsync(signature, cancellationToken);
            
            // Add additional metadata
            foreach (var metadata in request.AdditionalMetadata)
            {
                signatureMetadata.AdditionalMetadata[metadata.Key] = metadata.Value;
            }
            
            signatureMetadata.AdditionalMetadata["SigningReason"] = request.SigningReason;
            signatureMetadata.AdditionalMetadata["SigningUserId"] = request.SigningUserId;
            signatureMetadata.AdditionalMetadata["SigningUsername"] = request.SigningUsername;

            // Sign the audit record
            auditRecord.SignRecord(signature, signatureMetadata);

            // Update the audit record in the repository
            await _auditRepository.CreateAsync(auditRecord, cancellationToken);

            _logger.LogInformation("Successfully signed audit record {AuditRecordId} by user {SigningUserId}", 
                request.AuditRecordId, request.SigningUserId);

            return new SignAuditRecordResponse
            {
                Success = true,
                DigitalSignature = signature,
                SignatureMetadata = signatureMetadata,
                SignedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while signing audit record {AuditRecordId}", request.AuditRecordId);
            return SignAuditRecordResponse.Failure("Internal error occurred while signing audit record");
        }
    }

    private static List<string> ValidateRequest(SignAuditRecordCommand request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.AuditRecordId))
            errors.Add("Audit record ID is required");

        if (string.IsNullOrWhiteSpace(request.UserCertificate))
            errors.Add("User certificate is required");

        if (string.IsNullOrWhiteSpace(request.SigningUserId))
            errors.Add("Signing user ID is required");

        if (string.IsNullOrWhiteSpace(request.SigningUsername))
            errors.Add("Signing username is required");

        if (string.IsNullOrWhiteSpace(request.SigningReason))
            errors.Add("Signing reason is required");

        return errors;
    }
}