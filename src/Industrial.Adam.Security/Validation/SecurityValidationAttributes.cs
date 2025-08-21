using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Industrial.Adam.Security.Validation;

/// <summary>
/// Validation attribute to prevent SQL injection attacks
/// </summary>
public class NoSqlInjectionAttribute : ValidationAttribute
{
    private static readonly string[] SqlKeywords = new[]
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", "EXEC",
        "UNION", "SCRIPT", "JAVASCRIPT", "VBSCRIPT", "ONLOAD", "ONERROR", "ONCLICK"
    };

    // Simplified SQL injection pattern to prevent ReDoS attacks
    private static readonly Regex SqlInjectionPattern = new(
        @"\b(ALTER|CREATE|DELETE|DROP|EXEC|INSERT|MERGE|SELECT|UPDATE|UNION)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    // Additional simple patterns for common SQL injection attempts
    private static readonly string[] SimpleSqlPatterns = {
        "' OR 1=1", "' OR '1'='1", "\" OR 1=1", "\" OR \"1\"=\"1\"",
        "'; DROP TABLE", "'; DELETE FROM", "' UNION SELECT",
        "admin'--", "admin'#", "'/*", "*/'"
    };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        // Check for SQL injection patterns with timeout protection
        try
        {
            if (SqlInjectionPattern.IsMatch(stringValue))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially unsafe SQL patterns.");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Regex timeout indicates potential ReDoS attack
            return new ValidationResult($"The {validationContext.DisplayName} field contains content that cannot be validated safely.");
        }

        // Check simple patterns for performance
        var upperValue = stringValue.ToUpperInvariant();
        foreach (var pattern in SimpleSqlPatterns)
        {
            if (upperValue.Contains(pattern.ToUpperInvariant()))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially unsafe SQL patterns.");
            }
        }

        // Check for common SQL keywords in suspicious contexts
        foreach (var keyword in SqlKeywords)
        {
            if (upperValue.Contains(keyword) && HasSuspiciousContext(stringValue, keyword))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially unsafe content.");
            }
        }

        return ValidationResult.Success!;
    }

    private static bool HasSuspiciousContext(string value, string keyword)
    {
        var suspiciousPatterns = new[]
        {
            $"{keyword} *",
            $"' *{keyword}",
            $"; *{keyword}",
            $"-- *{keyword}",
            $"/* *{keyword}"
        };

        return suspiciousPatterns.Any(pattern =>
        {
            try
            {
                return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
            }
            catch (RegexMatchTimeoutException)
            {
                // Treat timeout as a match (suspicious)
                return true;
            }
        });
    }
}

/// <summary>
/// Validation attribute to prevent XSS attacks
/// </summary>
public class NoXssAttribute : ValidationAttribute
{
    // Simplified XSS pattern to prevent ReDoS attacks
    private static readonly Regex XssPattern = new(
        @"<\s*(script|iframe|object|embed|link|meta|style)\b|javascript\s*:|on\w+\s*=|eval\s*\(|expression\s*\(",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    private static readonly string[] XssKeywords = new[]
    {
        "javascript:", "vbscript:", "onload", "onerror", "onclick", "onmouseover",
        "onfocus", "onblur", "onchange", "onsubmit", "eval(", "expression(",
        "document.cookie", "window.location", "document.write"
    };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        // Check for XSS patterns with timeout protection
        try
        {
            if (XssPattern.IsMatch(stringValue))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially unsafe HTML/JavaScript content.");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Regex timeout indicates potential ReDoS attack
            return new ValidationResult($"The {validationContext.DisplayName} field contains content that cannot be validated safely.");
        }

        // Check for XSS keywords
        var lowerValue = stringValue.ToLowerInvariant();
        foreach (var keyword in XssKeywords)
        {
            if (lowerValue.Contains(keyword.ToLowerInvariant()))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains potentially unsafe content.");
            }
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute to prevent directory traversal attacks
/// </summary>
public class NoDirectoryTraversalAttribute : ValidationAttribute
{
    private static readonly string[] TraversalPatterns = new[]
    {
        "../", "..\\", "..%2f", "..%2F", "..%5c", "..%5C",
        "%2e%2e%2f", "%2e%2e%5c", "%2e%2e/", "%2e%2e\\",
        "..%252f", "..%252F", "..%255c", "..%255C"
    };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        var lowerValue = stringValue.ToLowerInvariant();

        foreach (var pattern in TraversalPatterns)
        {
            if (lowerValue.Contains(pattern.ToLowerInvariant()))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains invalid path characters.");
            }
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for safe file paths
/// </summary>
public class SafeFilePathAttribute : ValidationAttribute
{
    private static readonly Regex InvalidPathChars = new(
        @"[<>:""|?*\x00-\x1f]|^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    private static readonly char[] ForbiddenChars =
        Path.GetInvalidFileNameChars().Concat(Path.GetInvalidPathChars()).ToArray();

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        // Check for invalid path characters with timeout protection
        try
        {
            if (InvalidPathChars.IsMatch(stringValue))
            {
                return new ValidationResult($"The {validationContext.DisplayName} field contains invalid file path characters.");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field contains content that cannot be validated safely.");
        }

        // Check for forbidden characters
        if (stringValue.IndexOfAny(ForbiddenChars) >= 0)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field contains forbidden characters.");
        }

        // Check for directory traversal
        if (stringValue.Contains(".."))
        {
            return new ValidationResult($"The {validationContext.DisplayName} field contains invalid path traversal sequences.");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for industrial equipment IDs
/// </summary>
public class EquipmentIdAttribute : ValidationAttribute
{
    private static readonly Regex EquipmentIdPattern = new(
        @"^[A-Z0-9][A-Z0-9\-_]{2,19}$",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        try
        {
            if (!EquipmentIdPattern.IsMatch(stringValue))
            {
                return new ValidationResult($"The {validationContext.DisplayName} must be 3-20 characters, start with alphanumeric, and contain only letters, numbers, hyphens, and underscores.");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field contains content that cannot be validated safely.");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for work order numbers
/// </summary>
public class WorkOrderNumberAttribute : ValidationAttribute
{
    private static readonly Regex WorkOrderPattern = new(
        @"^WO[0-9]{6,12}$",
        RegexOptions.Compiled, TimeSpan.FromMilliseconds(50));

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        try
        {
            if (!WorkOrderPattern.IsMatch(stringValue))
            {
                return new ValidationResult($"The {validationContext.DisplayName} must be in format WO followed by 6-12 digits (e.g., WO123456).");
            }
        }
        catch (RegexMatchTimeoutException)
        {
            return new ValidationResult($"The {validationContext.DisplayName} field contains content that cannot be validated safely.");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for IP addresses
/// </summary>
public class ValidIpAddressAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        if (!System.Net.IPAddress.TryParse(stringValue, out var _))
        {
            return new ValidationResult($"The {validationContext.DisplayName} must be a valid IP address.");
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for safe JSON content
/// </summary>
public class SafeJsonAttribute : ValidationAttribute
{
    private static readonly string[] DangerousPatterns = new[]
    {
        "__proto__", "constructor", "prototype", "eval", "function",
        "script", "javascript:", "data:", "vbscript:"
    };

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || value is not string stringValue)
            return ValidationResult.Success!;

        // Basic JSON structure validation
        try
        {
            System.Text.Json.JsonDocument.Parse(stringValue);
        }
        catch (System.Text.Json.JsonException)
        {
            return new ValidationResult($"The {validationContext.DisplayName} must be valid JSON.");
        }

        // Check for dangerous patterns
        var lowerValue = stringValue.ToLowerInvariant();
        foreach (var pattern in DangerousPatterns)
        {
            if (lowerValue.Contains(pattern))
            {
                return new ValidationResult($"The {validationContext.DisplayName} contains unsafe JSON patterns.");
            }
        }

        return ValidationResult.Success!;
    }
}

/// <summary>
/// Validation attribute for rate limiting values
/// </summary>
public class RateLimitAttribute : ValidationAttribute
{
    public int MinValue { get; set; } = 1;
    public int MaxValue { get; set; } = 1000;

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null)
            return ValidationResult.Success!;

        if (value is not int intValue)
        {
            return new ValidationResult($"The {validationContext.DisplayName} must be a number.");
        }

        if (intValue < MinValue || intValue > MaxValue)
        {
            return new ValidationResult($"The {validationContext.DisplayName} must be between {MinValue} and {MaxValue}.");
        }

        return ValidationResult.Success!;
    }
}
