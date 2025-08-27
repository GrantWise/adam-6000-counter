using FluentValidation;
using Industrial.Adam.Security.Application.Commands.DigitalSignature;
using System.Text.RegularExpressions;

namespace Industrial.Adam.Security.Application.Validators;

/// <summary>
/// Validator for sign audit record command
/// </summary>
public class SignAuditRecordCommandValidator : AbstractValidator<SignAuditRecordCommand>
{
    public SignAuditRecordCommandValidator()
    {
        RuleFor(x => x.AuditRecordId)
            .NotEmpty().WithMessage("Audit record ID is required")
            .MaximumLength(50).WithMessage("Audit record ID cannot exceed 50 characters")
            .Must(BeValidGuid).WithMessage("Audit record ID must be a valid GUID");

        RuleFor(x => x.UserCertificate)
            .NotEmpty().WithMessage("User certificate is required")
            .MaximumLength(10000).WithMessage("Certificate data is too large")
            .Must(BeValidCertificate).WithMessage("Invalid certificate format");

        RuleFor(x => x.SigningUserId)
            .NotEmpty().WithMessage("Signing user ID is required")
            .MaximumLength(50).WithMessage("Signing user ID cannot exceed 50 characters")
            .Must(BeValidGuid).WithMessage("Signing user ID must be a valid GUID");

        RuleFor(x => x.SigningUsername)
            .NotEmpty().WithMessage("Signing username is required")
            .MaximumLength(100).WithMessage("Signing username cannot exceed 100 characters")
            .Must(BeValidUsername).WithMessage("Invalid username format");

        RuleFor(x => x.SigningReason)
            .NotEmpty().WithMessage("Signing reason is required")
            .MaximumLength(500).WithMessage("Signing reason cannot exceed 500 characters")
            .Must(NotContainSqlInjection).WithMessage("Signing reason contains invalid characters");

        RuleForEach(x => x.AdditionalMetadata.Keys)
            .MaximumLength(100).WithMessage("Metadata key cannot exceed 100 characters")
            .Must(BeValidMetadataKey).WithMessage("Invalid metadata key format");

        RuleForEach(x => x.AdditionalMetadata.Values)
            .MaximumLength(1000).WithMessage("Metadata value cannot exceed 1000 characters")
            .Must(NotContainSqlInjection).WithMessage("Metadata value contains invalid characters");
    }

    private static bool BeValidGuid(string? value)
    {
        return value != null && Guid.TryParse(value, out _);
    }

    private static bool BeValidCertificate(string? certificate)
    {
        if (string.IsNullOrEmpty(certificate))
            return false;

        // Check for PEM format
        if (certificate.Contains("-----BEGIN CERTIFICATE-----") && certificate.Contains("-----END CERTIFICATE-----"))
            return true;

        // Check for Base64 format (basic validation)
        try
        {
            Convert.FromBase64String(certificate);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool BeValidUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
            return false;

        // Username should contain only alphanumeric characters, dots, underscores, and hyphens
        var usernameRegex = new Regex(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);
        return usernameRegex.IsMatch(username);
    }

    private static bool BeValidMetadataKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        // Metadata keys should contain only alphanumeric characters and underscores
        var keyRegex = new Regex(@"^[a-zA-Z0-9_]+$", RegexOptions.Compiled);
        return keyRegex.IsMatch(key);
    }

    private static bool NotContainSqlInjection(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        // Check for common SQL injection patterns
        var sqlInjectionPatterns = new[]
        {
            @"('|('')|(\s*;\s*)|(\s*--)|(\s*/\*)|(\*/\s*)",
            @"\b(ALTER|CREATE|DELETE|DROP|EXEC|EXECUTE|INSERT|SELECT|UNION|UPDATE)\b",
            @"\b(OR|AND)\s+\w*\s*=\s*\w*",
            @"'[^']*'|""[^""]*""",
            @"\bXP_\w+",
            @"\bCMDSHELL\b"
        };

        var combinedPattern = string.Join("|", sqlInjectionPatterns);
        var sqlInjectionRegex = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return !sqlInjectionRegex.IsMatch(value);
    }
}

/// <summary>
/// Base validator with common security rules
/// </summary>
public abstract class SecurityValidatorBase<T> : AbstractValidator<T>
{
    protected IRuleBuilderOptions<T, string?> MustNotContainSqlInjection<TProperty>(
        IRuleBuilder<T, string?> ruleBuilder,
        string propertyName = "")
    {
        return ruleBuilder.Must(NotContainSqlInjection)
            .WithMessage($"{propertyName} contains potentially dangerous SQL characters");
    }

    protected IRuleBuilderOptions<T, string?> MustNotContainXss<TProperty>(
        IRuleBuilder<T, string?> ruleBuilder,
        string propertyName = "")
    {
        return ruleBuilder.Must(NotContainXss)
            .WithMessage($"{propertyName} contains potentially dangerous script content");
    }

    protected IRuleBuilderOptions<T, string?> MustNotContainPathTraversal<TProperty>(
        IRuleBuilder<T, string?> ruleBuilder,
        string propertyName = "")
    {
        return ruleBuilder.Must(NotContainPathTraversal)
            .WithMessage($"{propertyName} contains potentially dangerous path characters");
    }

    protected IRuleBuilderOptions<T, string?> MustNotContainCommandInjection<TProperty>(
        IRuleBuilder<T, string?> ruleBuilder,
        string propertyName = "")
    {
        return ruleBuilder.Must(NotContainCommandInjection)
            .WithMessage($"{propertyName} contains potentially dangerous command characters");
    }

    private static bool NotContainSqlInjection(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        var sqlInjectionPatterns = new[]
        {
            @"('|('')|(\s*;\s*)|(\s*--)|(\s*/\*)|(\*/\s*)",
            @"\b(ALTER|CREATE|DELETE|DROP|EXEC|EXECUTE|INSERT|SELECT|UNION|UPDATE)\b",
            @"\b(OR|AND)\s+['""]*\w*['""]*\s*=\s*['""]*\w*['""]*",
            @"'[^']*'|""[^""]*""",
            @"\bXP_\w+",
            @"\bCMDSHELL\b",
            @"(\%27)|(\%22)|(\%3B)|(\%3C)|(\%3E)",
            @"(0x[0-9A-Fa-f]+)"
        };

        var combinedPattern = string.Join("|", sqlInjectionPatterns);
        var sqlInjectionRegex = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return !sqlInjectionRegex.IsMatch(value);
    }

    private static bool NotContainXss(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        var xssPatterns = new[]
        {
            @"<\s*script[^>]*>.*?</\s*script\s*>",
            @"<\s*iframe[^>]*>.*?</\s*iframe\s*>",
            @"<\s*object[^>]*>.*?</\s*object\s*>",
            @"<\s*embed[^>]*>",
            @"<\s*link[^>]*>",
            @"javascript\s*:",
            @"vbscript\s*:",
            @"on\w+\s*=",
            @"(\%3C)|(\%3E)|(\%22)|(\%27)|(\%3B)",
            @"&lt;|&gt;|&quot;|&#x27;|&#x3B;"
        };

        var combinedPattern = string.Join("|", xssPatterns);
        var xssRegex = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return !xssRegex.IsMatch(value);
    }

    private static bool NotContainPathTraversal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        var pathTraversalPatterns = new[]
        {
            @"\.\.(/|\\)",
            @"(~|%7E)",
            @"(\%2E\%2E\%2F)|(\%2E\%2E\%5C)",
            @"(\.\.%2F)|(\.\.%5C)",
            @"/etc/passwd",
            @"\\windows\\system32"
        };

        var combinedPattern = string.Join("|", pathTraversalPatterns);
        var pathTraversalRegex = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return !pathTraversalRegex.IsMatch(value);
    }

    private static bool NotContainCommandInjection(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        var commandInjectionPatterns = new[]
        {
            @"[;&|`$(){}[\]\\]",
            @"\b(cat|ls|dir|type|copy|del|rm|mv|cp|chmod|chown|wget|curl|ping|nslookup|netstat|ps|kill|whoami|id|uname)\b",
            @"(\$\()|(\$\{)|(\`)",
            @"(\|\s*\w+)|(\&\&\s*\w+)|(\;\s*\w+)",
            @"(\%0A)|(\%0D)|(\%09)",
            @"\\x[0-9A-Fa-f]{2}"
        };

        var combinedPattern = string.Join("|", commandInjectionPatterns);
        var commandInjectionRegex = new Regex(combinedPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        return !commandInjectionRegex.IsMatch(value);
    }
}

/// <summary>
/// Generic input sanitization and validation utilities
/// </summary>
public static class InputSanitizer
{
    /// <summary>
    /// Sanitizes input by removing or encoding dangerous characters
    /// </summary>
    public static string SanitizeInput(string? input, SanitizationLevel level = SanitizationLevel.Standard)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var sanitized = input;

        // Remove null bytes
        sanitized = sanitized.Replace('\0', ' ');

        switch (level)
        {
            case SanitizationLevel.Minimal:
                // Only remove the most dangerous characters
                sanitized = Regex.Replace(sanitized, @"[<>""']", "", RegexOptions.Compiled);
                break;

            case SanitizationLevel.Standard:
                // Remove common injection patterns
                sanitized = Regex.Replace(sanitized, @"[<>""';\\&|`$(){}[\]]", "", RegexOptions.Compiled);
                break;

            case SanitizationLevel.Strict:
                // Allow only alphanumeric, spaces, and basic punctuation
                sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9\s\.\,\-\_]", "", RegexOptions.Compiled);
                break;

            case SanitizationLevel.AlphanumericOnly:
                // Allow only alphanumeric characters
                sanitized = Regex.Replace(sanitized, @"[^a-zA-Z0-9]", "", RegexOptions.Compiled);
                break;
        }

        // Limit length to prevent DoS attacks
        const int maxLength = 10000;
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized[..maxLength];
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Validates that input meets security requirements
    /// </summary>
    public static ValidationResult ValidateInput(string? input, string fieldName = "Input")
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(input))
        {
            return result; // Allow empty input
        }

        // Check for SQL injection patterns
        if (ContainsSqlInjection(input))
        {
            result.Errors.Add($"{fieldName} contains potentially dangerous SQL patterns");
        }

        // Check for XSS patterns
        if (ContainsXss(input))
        {
            result.Errors.Add($"{fieldName} contains potentially dangerous script content");
        }

        // Check for path traversal
        if (ContainsPathTraversal(input))
        {
            result.Errors.Add($"{fieldName} contains potentially dangerous path traversal patterns");
        }

        // Check for command injection
        if (ContainsCommandInjection(input))
        {
            result.Errors.Add($"{fieldName} contains potentially dangerous command patterns");
        }

        // Check length
        if (input.Length > 10000)
        {
            result.Errors.Add($"{fieldName} exceeds maximum allowed length");
        }

        result.IsValid = !result.Errors.Any();
        return result;
    }

    private static bool ContainsSqlInjection(string input)
    {
        var patterns = new[]
        {
            @"('|('')|(\s*;\s*)|(\s*--)|(\s*/\*)|(\*/\s*)",
            @"\b(ALTER|CREATE|DELETE|DROP|EXEC|EXECUTE|INSERT|SELECT|UNION|UPDATE)\b",
            @"\bXP_\w+"
        };

        return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private static bool ContainsXss(string input)
    {
        var patterns = new[]
        {
            @"<\s*script",
            @"<\s*iframe",
            @"javascript\s*:",
            @"on\w+\s*="
        };

        return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private static bool ContainsPathTraversal(string input)
    {
        var patterns = new[]
        {
            @"\.\.(/|\\)",
            @"/etc/passwd",
            @"\\windows\\system32"
        };

        return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }

    private static bool ContainsCommandInjection(string input)
    {
        var patterns = new[]
        {
            @"[;&|`]",
            @"\$\(",
            @"\b(cat|ls|rm|cp|wget|curl)\b"
        };

        return patterns.Any(pattern => Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase));
    }
}

/// <summary>
/// Sanitization levels for different security contexts
/// </summary>
public enum SanitizationLevel
{
    Minimal = 1,
    Standard = 2,
    Strict = 3,
    AlphanumericOnly = 4
}

/// <summary>
/// Input validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}