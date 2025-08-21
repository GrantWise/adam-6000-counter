using System.Text;
using System.Text.Json;
using Industrial.Adam.Security.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Validation;

/// <summary>
/// Middleware for input validation and sanitization
/// </summary>
public class InputValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InputValidationMiddleware> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly InputValidationOptions _options;

    public InputValidationMiddleware(
        RequestDelegate next,
        ILogger<InputValidationMiddleware> logger,
        SecurityEventLogger securityLogger,
        InputValidationOptions? options = null)
    {
        _next = next;
        _logger = logger;
        _securityLogger = securityLogger;
        _options = options ?? new InputValidationOptions();
    }

    /// <summary>
    /// Validates and sanitizes HTTP request inputs
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var ipAddress = GetClientIpAddress(context);
        var username = context.User?.Identity?.Name;

        try
        {
            // Validate request size
            if (context.Request.ContentLength > _options.MaxRequestSize)
            {
                _securityLogger.LogValidationFailure(
                    "RequestSize",
                    context.Request.ContentLength?.ToString(),
                    $"Request size exceeds maximum allowed size of {_options.MaxRequestSize} bytes",
                    ipAddress,
                    username);

                context.Response.StatusCode = 413; // Request Entity Too Large
                await context.Response.WriteAsync("Request too large");
                return;
            }

            // Validate query string parameters
            await ValidateQueryParameters(context, ipAddress, username);

            // Validate headers
            ValidateHeaders(context, ipAddress, username);

            // For POST/PUT requests, validate body content
            if (context.Request.Method == "POST" || context.Request.Method == "PUT")
            {
                await ValidateRequestBody(context, ipAddress, username);
            }

            // Validate file uploads
            if (context.Request.HasFormContentType && context.Request.Form.Files.Any())
            {
                ValidateFileUploads(context, ipAddress, username);
            }

            await _next(context);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Input validation failed for request from {IpAddress}", ipAddress);

            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var response = JsonSerializer.Serialize(new
            {
                error = "Invalid input",
                message = ex.Message,
                timestamp = DateTimeOffset.UtcNow
            });

            await context.Response.WriteAsync(response);
        }
    }

    /// <summary>
    /// Validates query string parameters
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    /// <returns>Task</returns>
    private Task ValidateQueryParameters(HttpContext context, string? ipAddress, string? username)
    {
        foreach (var parameter in context.Request.Query)
        {
            var key = parameter.Key;
            var values = parameter.Value.ToArray();

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                // Check parameter length
                if (value.Length > _options.MaxParameterLength)
                {
                    _securityLogger.LogValidationFailure(
                        key,
                        value,
                        $"Parameter value exceeds maximum length of {_options.MaxParameterLength}",
                        ipAddress,
                        username);

                    throw new ValidationException($"Parameter '{key}' value is too long");
                }

                // Check for dangerous patterns
                if (ContainsDangerousPatterns(value))
                {
                    _securityLogger.LogValidationFailure(
                        key,
                        value,
                        "Parameter contains potentially dangerous patterns",
                        ipAddress,
                        username);

                    throw new ValidationException($"Parameter '{key}' contains invalid characters");
                }

                // Validate specific parameter types
                ValidateSpecialParameters(key, value, ipAddress, username);
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Validates HTTP headers
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    private void ValidateHeaders(HttpContext context, string? ipAddress, string? username)
    {
        foreach (var header in context.Request.Headers)
        {
            var key = header.Key;
            var values = header.Value.ToArray();

            foreach (var value in values)
            {
                if (string.IsNullOrEmpty(value))
                    continue;

                // Check header length
                if (value.Length > _options.MaxHeaderLength)
                {
                    _securityLogger.LogValidationFailure(
                        key,
                        value,
                        $"Header value exceeds maximum length of {_options.MaxHeaderLength}",
                        ipAddress,
                        username);

                    throw new ValidationException($"Header '{key}' value is too long");
                }

                // Check for dangerous patterns in headers
                if (ContainsDangerousPatterns(value))
                {
                    _securityLogger.LogValidationFailure(
                        key,
                        value,
                        "Header contains potentially dangerous patterns",
                        ipAddress,
                        username);

                    throw new ValidationException($"Header '{key}' contains invalid characters");
                }

                // Validate specific headers
                ValidateSpecialHeaders(key, value, ipAddress, username);
            }
        }
    }

    /// <summary>
    /// Validates request body content using streaming to prevent memory exhaustion
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    /// <returns>Task</returns>
    private async Task ValidateRequestBody(HttpContext context, string? ipAddress, string? username)
    {
        const int maxBufferSize = 102400; // 100KB limit for validation
        const int chunkSize = 4096; // 4KB chunks for streaming

        if (context.Request.ContentLength == 0)
            return;

        var contentType = context.Request.ContentType?.ToLowerInvariant() ?? "";
        var buffer = new byte[Math.Min(chunkSize, maxBufferSize)];
        var totalBytesRead = 0;
        var bodyBuilder = new StringBuilder(maxBufferSize);

        // Enable buffering for the request body if not already enabled
        if (!context.Request.Body.CanSeek)
        {
            context.Request.EnableBuffering();
        }

        context.Request.Body.Position = 0;

        try
        {
            int bytesRead;
            while ((bytesRead = await context.Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                totalBytesRead += bytesRead;

                // Enforce memory limit to prevent exhaustion attacks
                if (totalBytesRead > maxBufferSize)
                {
                    _securityLogger.LogValidationFailure(
                        "RequestBody",
                        $"Size: {totalBytesRead} bytes",
                        "Request body exceeds validation buffer limit",
                        ipAddress,
                        username);

                    throw new ValidationException("Request body too large for validation");
                }

                // Convert chunk to string for pattern analysis
                var chunkText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                bodyBuilder.Append(chunkText);

                // Perform streaming validation on each chunk
                if (ContainsDangerousPatterns(chunkText))
                {
                    _securityLogger.LogValidationFailure(
                        "RequestBody",
                        SanitizeForLogging(chunkText),
                        "Request body chunk contains potentially dangerous patterns",
                        ipAddress,
                        username);

                    throw new ValidationException("Request body contains invalid content");
                }
            }

            // Reset stream position
            context.Request.Body.Position = 0;

            // Validate JSON structure if content type is JSON
            if (contentType.Contains("application/json") && bodyBuilder.Length > 0)
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(bodyBuilder.ToString());
                    // JSON parsed successfully, no action needed
                }
                catch (JsonException ex)
                {
                    _securityLogger.LogValidationFailure(
                        "RequestBody",
                        "[JSON_PARSING_ERROR]",
                        $"Invalid JSON format: {ex.Message}",
                        ipAddress,
                        username);

                    throw new ValidationException("Request body contains invalid JSON");
                }
            }
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Error during streaming body validation from {IpAddress}", ipAddress);
            throw new ValidationException("Error validating request body");
        }
    }

    /// <summary>
    /// Sanitizes content for safe logging by truncating and removing sensitive patterns
    /// </summary>
    /// <param name="content">Content to sanitize</param>
    /// <returns>Sanitized content</returns>
    private static string SanitizeForLogging(string content)
    {
        if (string.IsNullOrEmpty(content))
            return "[EMPTY]";

        // Truncate long content
        var sanitized = content.Length > 200 ? content[..200] + "..." : content;

        // Remove potential passwords or tokens
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(password|token|key|secret|auth)\s*[:=]\s*[^\s&,}]+",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// Validates file uploads
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    private void ValidateFileUploads(HttpContext context, string? ipAddress, string? username)
    {
        foreach (var file in context.Request.Form.Files)
        {
            // Validate file size
            if (file.Length > _options.MaxFileSize)
            {
                _securityLogger.LogValidationFailure(
                    "FileSize",
                    file.Length.ToString(),
                    $"File '{file.FileName}' exceeds maximum size of {_options.MaxFileSize} bytes",
                    ipAddress,
                    username);

                throw new ValidationException($"File '{file.FileName}' is too large");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(extension) &&
                _options.AllowedFileExtensions.Any() &&
                !_options.AllowedFileExtensions.Contains(extension))
            {
                _securityLogger.LogValidationFailure(
                    "FileExtension",
                    extension,
                    $"File '{file.FileName}' has disallowed extension",
                    ipAddress,
                    username);

                throw new ValidationException($"File type '{extension}' is not allowed");
            }

            // Validate file name
            if (ContainsDangerousPatterns(file.FileName))
            {
                _securityLogger.LogValidationFailure(
                    "FileName",
                    file.FileName,
                    "File name contains potentially dangerous patterns",
                    ipAddress,
                    username);

                throw new ValidationException("File name contains invalid characters");
            }
        }
    }

    /// <summary>
    /// Checks if input contains dangerous patterns using comprehensive detection
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if dangerous patterns found</returns>
    private static bool ContainsDangerousPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        // Normalize input for analysis
        var normalizedInput = NormalizeForAnalysis(input);

        // Check various attack patterns
        return ContainsSqlInjectionPatterns(normalizedInput) ||
               ContainsXssPatterns(normalizedInput) ||
               ContainsDirectoryTraversalPatterns(normalizedInput) ||
               ContainsCommandInjectionPatterns(normalizedInput) ||
               ContainsEncodedAttackPatterns(normalizedInput);
    }

    /// <summary>
    /// Normalizes input for security analysis (handles encoding, etc.)
    /// </summary>
    /// <param name="input">Input to normalize</param>
    /// <returns>Normalized input</returns>
    private static string NormalizeForAnalysis(string input)
    {
        // URL decode multiple times to handle double/triple encoding
        var normalized = input;
        for (int i = 0; i < 3; i++)
        {
            var decoded = System.Web.HttpUtility.UrlDecode(normalized);
            if (decoded == normalized)
                break;
            normalized = decoded;
        }

        // HTML decode
        normalized = System.Web.HttpUtility.HtmlDecode(normalized);

        // Remove null bytes and control characters that might hide attacks
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", "");

        // Normalize whitespace that might be used to evade detection
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");

        return normalized.Trim();
    }

    /// <summary>
    /// Checks for SQL injection patterns with context awareness
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if SQL injection patterns found</returns>
    private static bool ContainsSqlInjectionPatterns(string input)
    {
        var lowerInput = input.ToLowerInvariant();

        // Classic SQL injection patterns
        var sqlPatterns = new[]
        {
            // Union-based attacks
            "union select", "union all select",
            
            // Boolean-based attacks
            "' or 1=1", "' or '1'='1", "\" or 1=1", "\" or \"1\"=\"1\"",
            "' or true", "' or 1", "') or 1=1", "\") or 1=1",
            
            // Time-based attacks
            "waitfor delay", "pg_sleep", "sleep(", "benchmark(",
            
            // Error-based attacks
            "extractvalue(", "updatexml(", "and(select*from",
            
            // Stacked queries
            "'; drop", "'; delete", "'; insert", "'; update",
            "'; exec", "'; create", "'; alter",
            
            // System functions
            "xp_cmdshell", "sp_executesql", "openrowset", "opendatasource",
            
            // Database fingerprinting
            "@@version", "version()", "user()", "database()",
            
            // Comment-based evasion
            "/*", "*/", "#", "-- ", "--+",
            
            // Hex encoding
            "0x", "char(",
            
            // Conditional statements
            "if(", "case when", "having 1=1"
        };

        // Check for SQL keywords in suspicious contexts
        var suspiciousContexts = new[]
        {
            @"'\s*or\s+", @"""\s*or\s+", @"'\s*and\s+", @"""\s*and\s+",
            @";\s*(drop|delete|insert|update|create|alter|exec)",
            @"(union|select|from|where|order\s+by|group\s+by|having|into|values)",
            @"(information_schema|sysobjects|syscolumns|sys\.tables)"
        };

        foreach (var pattern in sqlPatterns)
        {
            if (lowerInput.Contains(pattern))
                return true;
        }

        // Check regex patterns for more complex cases
        foreach (var pattern in suspiciousContexts)
        {
            try
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(lowerInput, pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase,
                    TimeSpan.FromMilliseconds(50)))
                {
                    return true;
                }
            }
            catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
            {
                // Treat timeout as suspicious
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks for XSS patterns
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if XSS patterns found</returns>
    private static bool ContainsXssPatterns(string input)
    {
        var lowerInput = input.ToLowerInvariant();

        var xssPatterns = new[]
        {
            // Script tags
            "<script", "</script>", "javascript:", "vbscript:",
            
            // Event handlers
            "onload=", "onerror=", "onclick=", "onmouseover=", "onfocus=",
            "onblur=", "onchange=", "onsubmit=", "onkeydown=", "onkeyup=",
            
            // Dangerous functions
            "eval(", "expression(", "document.cookie", "window.location",
            "document.write", "document.writeln", "innerhtml", "outerhtml",
            
            // Other dangerous elements
            "<iframe", "<object", "<embed", "<link", "<meta", "<style",
            "<form", "data:", "about:",
            
            // CSS expressions
            "expression(", "url(", "-moz-binding",
            
            // SVG attacks
            "<svg", "onload", "<animatetransform",
            
            // Base64/hex encoded
            "&#", "%3c", "%3e", "%22", "%27"
        };

        return xssPatterns.Any(pattern => lowerInput.Contains(pattern));
    }

    /// <summary>
    /// Checks for directory traversal patterns
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if directory traversal patterns found</returns>
    private static bool ContainsDirectoryTraversalPatterns(string input)
    {
        var patterns = new[]
        {
            "../", "..\\", "..%2f", "..%2F", "..%5c", "..%5C",
            "%2e%2e%2f", "%2e%2e%5c", "%2e%2e/", "%2e%2e\\",
            "..%252f", "..%252F", "..%255c", "..%255C",
            "..\\/", "\\..\\", "/..", "\\..",
            "%c0%ae%c0%ae/", "%c1%1c", "..%c0%af", "..%ef%bc%8f"
        };

        var lowerInput = input.ToLowerInvariant();
        return patterns.Any(pattern => lowerInput.Contains(pattern.ToLowerInvariant()));
    }

    /// <summary>
    /// Checks for command injection patterns
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if command injection patterns found</returns>
    private static bool ContainsCommandInjectionPatterns(string input)
    {
        var patterns = new[]
        {
            // Unix/Linux commands
            "; rm ", "; cat ", "; ls ", "; ps ", "; kill ", "; wget ", "; curl ",
            "| nc ", "| netcat ", "| sh ", "| bash ", "| zsh ",
            "&& rm ", "&& cat ", "&& wget ", "&& curl ",
            "|| rm ", "|| cat ", "|| wget ", "|| curl ",
            "`rm ", "`cat ", "`wget ", "`curl ", "`sh ", "`bash ",
            "$(rm", "$(cat", "$(wget", "$(curl", "$(sh", "$(bash",
            
            // Windows commands
            "; del ", "; dir ", "; type ", "; copy ", "; move ", "; net ",
            "&& del ", "&& dir ", "&& type ", "&& copy ", "&& net ",
            "|| del ", "|| dir ", "|| type ", "|| copy ", "|| net ",
            
            // Shell variables and special chars
            "$IFS", "${IFS}", "$PATH", "${PATH}", "$HOME", "${HOME}",
            "%SystemRoot%", "%USERPROFILE%", "%TEMP%",
            
            // Process substitution
            "<(", ">(", "$(",
            
            // Here documents
            "<<EOF", "<<END"
        };

        var lowerInput = input.ToLowerInvariant();
        return patterns.Any(pattern => lowerInput.Contains(pattern.ToLowerInvariant()));
    }

    /// <summary>
    /// Checks for encoded attack patterns
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if encoded attack patterns found</returns>
    private static bool ContainsEncodedAttackPatterns(string input)
    {
        // Look for patterns that might indicate encoded payloads
        var encodingPatterns = new[]
        {
            // High concentration of URL encoding
            "%20%20%20", "%3c%2f", "%3e%3c",
            
            // Base64 padding patterns (might indicate encoded payload)
            "==%", "==&", "==%20",
            
            // Unicode encoding
            "\\u003c", "\\u003e", "\\u0027", "\\u0022",
            
            // Double encoding
            "%253c", "%253e", "%2527", "%2522",
            
            // Null byte variants
            "%00", "\\x00", "\\0",
            
            // LDAP injection
            ")(", ")(&", ")(!",
            
            // XPath injection
            "' or ", "'] | //", "//user"
        };

        var lowerInput = input.ToLowerInvariant();
        return encodingPatterns.Any(pattern => lowerInput.Contains(pattern));
    }

    /// <summary>
    /// Validates special parameters with specific rules
    /// </summary>
    /// <param name="key">Parameter key</param>
    /// <param name="value">Parameter value</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    private void ValidateSpecialParameters(string key, string value, string? ipAddress, string? username)
    {
        key = key.ToLowerInvariant();

        // Validate equipment IDs
        if (key.Contains("equipment") && key.Contains("id"))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^[A-Z0-9][A-Z0-9\-_]{2,19}$"))
            {
                _securityLogger.LogValidationFailure(
                    key,
                    value,
                    "Invalid equipment ID format",
                    ipAddress,
                    username);

                throw new ValidationException($"Parameter '{key}' has invalid format");
            }
        }

        // Validate work order numbers
        if (key.Contains("workorder") || key.Contains("work_order"))
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(value, @"^WO[0-9]{6,12}$"))
            {
                _securityLogger.LogValidationFailure(
                    key,
                    value,
                    "Invalid work order format",
                    ipAddress,
                    username);

                throw new ValidationException($"Parameter '{key}' has invalid work order format");
            }
        }
    }

    /// <summary>
    /// Validates special headers with specific rules
    /// </summary>
    /// <param name="key">Header key</param>
    /// <param name="value">Header value</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <param name="username">Username if available</param>
    private void ValidateSpecialHeaders(string key, string value, string? ipAddress, string? username)
    {
        key = key.ToLowerInvariant();

        // Validate Content-Type header
        if (key == "content-type")
        {
            var allowedContentTypes = new[]
            {
                "application/json", "application/x-www-form-urlencoded",
                "multipart/form-data", "text/plain", "text/html"
            };

            if (!allowedContentTypes.Any(ct => value.ToLowerInvariant().StartsWith(ct)))
            {
                _securityLogger.LogValidationFailure(
                    key,
                    value,
                    "Disallowed content type",
                    ipAddress,
                    username);

                throw new ValidationException("Content type not allowed");
            }
        }

        // Validate Authorization header format
        if (key == "authorization" && !value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            _securityLogger.LogValidationFailure(
                key,
                "[REDACTED]",
                "Invalid authorization header format",
                ipAddress,
                username);

            throw new ValidationException("Invalid authorization format");
        }
    }

    /// <summary>
    /// Gets client IP address from context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP address</returns>
    private static string? GetClientIpAddress(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
               context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
               context.Connection.RemoteIpAddress?.ToString();
    }
}

/// <summary>
/// Options for input validation middleware
/// </summary>
public class InputValidationOptions
{
    /// <summary>
    /// Maximum request size in bytes
    /// </summary>
    public long MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum parameter length
    /// </summary>
    public int MaxParameterLength { get; set; } = 4096;

    /// <summary>
    /// Maximum header length
    /// </summary>
    public int MaxHeaderLength { get; set; } = 8192;

    /// <summary>
    /// Maximum file size for uploads
    /// </summary>
    public long MaxFileSize { get; set; } = 50 * 1024 * 1024; // 50MB

    /// <summary>
    /// Maximum request body size for validation in bytes (default 100KB)
    /// </summary>
    public int MaxValidationBufferSize { get; set; } = 102400;

    /// <summary>
    /// Chunk size for streaming validation in bytes (default 4KB)
    /// </summary>
    public int ValidationChunkSize { get; set; } = 4096;

    /// <summary>
    /// Allowed file extensions for uploads
    /// </summary>
    public string[] AllowedFileExtensions { get; set; } = new[]
    {
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".txt", ".csv", ".json", ".xml"
    };
}

/// <summary>
/// Exception thrown when input validation fails
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
