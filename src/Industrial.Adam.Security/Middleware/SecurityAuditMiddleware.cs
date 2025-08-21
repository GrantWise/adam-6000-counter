using System.Diagnostics;
using System.Security.Claims;
using System.Text;
using Industrial.Adam.Security.Logging;
using Industrial.Adam.Security.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Middleware;

/// <summary>
/// Middleware for auditing security events across HTTP requests
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;
    private readonly SecurityEventLogger _securityLogger;

    public SecurityAuditMiddleware(
        RequestDelegate next,
        ILogger<SecurityAuditMiddleware> logger,
        SecurityEventLogger securityLogger)
    {
        _next = next;
        _logger = logger;
        _securityLogger = securityLogger;
    }

    /// <summary>
    /// Processes HTTP request and captures security events
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();

        // Add correlation ID to response headers for tracing
        context.Response.Headers.TryAdd("X-Correlation-ID", correlationId);

        var ipAddress = GetClientIpAddress(context);
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var username = context.User?.Identity?.Name;
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        try
        {
            // Process the request
            await _next(context);

            stopwatch.Stop();

            // Log successful request if it's a sensitive endpoint
            if (IsSensitiveEndpoint(path) && context.Response.StatusCode < 400)
            {
                LogDataAccessEvent(username, path, method, ipAddress, correlationId, context.Response.StatusCode);
            }

            // Detect suspicious patterns
            DetectSuspiciousPatterns(context, ipAddress, username, path, method, stopwatch.ElapsedMilliseconds);
        }
        catch (UnauthorizedAccessException ex)
        {
            stopwatch.Stop();
            LogAuthorizationFailure(context, username, path, method, ipAddress, ex);
            throw;
        }
        catch (SecurityException ex)
        {
            stopwatch.Stop();
            LogSecurityViolation(username, path, method, ipAddress, ex, correlationId);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Sanitize exception details for secure logging
            var sanitizedException = SanitizeException(ex);
            _logger.LogError(sanitizedException, "Unhandled exception in security audit middleware");

            throw;
        }
        finally
        {
            // Log response timing for performance monitoring
            if (stopwatch.ElapsedMilliseconds > 5000) // Log slow requests
            {
                _securityLogger.LogSuspiciousActivity(
                    "SlowRequest",
                    $"Request to {path} took {stopwatch.ElapsedMilliseconds}ms",
                    ipAddress,
                    username,
                    25,
                    new Dictionary<string, object>
                    {
                        ["Path"] = path,
                        ["Method"] = method,
                        ["Duration"] = stopwatch.ElapsedMilliseconds,
                        ["StatusCode"] = context.Response.StatusCode
                    });
            }
        }
    }

    /// <summary>
    /// Gets client IP address from HTTP context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client IP address</returns>
    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first (reverse proxy scenarios)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Determines if an endpoint is sensitive and should be audited
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if sensitive endpoint</returns>
    private static bool IsSensitiveEndpoint(string path)
    {
        var sensitivePaths = new[]
        {
            "/api/auth",
            "/api/admin",
            "/api/config",
            "/api/users",
            "/api/oee",
            "/api/equipment",
            "/api/work-orders",
            "/api/logger",
            "/health/detailed"
        };

        return sensitivePaths.Any(sensitivePath =>
            path.StartsWith(sensitivePath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Logs authorization failure event
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="username">Username</param>
    /// <param name="path">Request path</param>
    /// <param name="method">HTTP method</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="exception">Authorization exception</param>
    private void LogAuthorizationFailure(
        HttpContext context,
        string? username,
        string path,
        string method,
        string? ipAddress,
        Exception exception)
    {
        var requiredRole = ExtractRequiredRole(context);
        var userRole = ExtractUserRole(context);

        _securityLogger.LogAuthorizationFailure(
            username,
            path,
            requiredRole,
            userRole,
            method,
            ipAddress);
    }

    /// <summary>
    /// Logs data access event for sensitive endpoints
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="path">Request path</param>
    /// <param name="method">HTTP method</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="statusCode">Response status code</param>
    private void LogDataAccessEvent(
        string? username,
        string path,
        string method,
        string? ipAddress,
        string correlationId,
        int statusCode)
    {
        var securityEvent = new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.DataAccess,
            Severity = SecurityEventSeverity.Information,
            Username = username,
            IpAddress = ipAddress,
            Resource = path,
            HttpMethod = method,
            StatusCode = statusCode,
            Description = $"Data access: {method} {path}",
            RiskScore = 5,
            Metadata = new Dictionary<string, object>
            {
                ["AccessType"] = "API",
                ["StatusCode"] = statusCode,
                ["Sensitive"] = true
            }
        };

        _securityLogger.LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Logs security policy violation
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="path">Request path</param>
    /// <param name="method">HTTP method</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="exception">Security exception</param>
    /// <param name="correlationId">Correlation ID</param>
    private void LogSecurityViolation(
        string? username,
        string path,
        string method,
        string? ipAddress,
        Exception exception,
        string correlationId)
    {
        var securityEvent = new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.PolicyViolation,
            Severity = SecurityEventSeverity.High,
            Username = username,
            IpAddress = ipAddress,
            Resource = path,
            HttpMethod = method,
            Description = $"Security policy violation: {SanitizeExceptionMessage(exception.Message)}",
            ExceptionDetails = SanitizeExceptionDetails(exception),
            RiskScore = 75,
            Metadata = new Dictionary<string, object>
            {
                ["ViolationType"] = exception.GetType().Name,
                ["PolicyType"] = "Security"
            }
        };

        _securityLogger.LogSecurityEvent(securityEvent);
    }

    /// <summary>
    /// Detects suspicious patterns in requests
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="ipAddress">IP address</param>
    /// <param name="username">Username</param>
    /// <param name="path">Request path</param>
    /// <param name="method">HTTP method</param>
    /// <param name="duration">Request duration in milliseconds</param>
    private void DetectSuspiciousPatterns(
        HttpContext context,
        string? ipAddress,
        string? username,
        string path,
        string method,
        long duration)
    {
        // Check for SQL injection patterns in query parameters
        if (HasSqlInjectionPatterns(context.Request.QueryString.Value))
        {
            _securityLogger.LogSuspiciousActivity(
                "SQLInjectionAttempt",
                $"Possible SQL injection attempt detected in request to {path}",
                ipAddress,
                username,
                80,
                new Dictionary<string, object>
                {
                    ["Path"] = path,
                    ["Method"] = method,
                    ["QueryString"] = SanitizeQueryString(context.Request.QueryString.Value) ?? "[Empty]"
                });
        }

        // Check for XSS patterns in headers or query parameters
        if (HasXssPatterns(context.Request))
        {
            _securityLogger.LogSuspiciousActivity(
                "XSSAttempt",
                $"Possible XSS attempt detected in request to {path}",
                ipAddress,
                username,
                70,
                new Dictionary<string, object>
                {
                    ["Path"] = path,
                    ["Method"] = method,
                    ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
                });
        }

        // Check for directory traversal patterns
        if (HasDirectoryTraversalPatterns(path))
        {
            _securityLogger.LogSuspiciousActivity(
                "DirectoryTraversalAttempt",
                $"Possible directory traversal attempt: {path}",
                ipAddress,
                username,
                85,
                new Dictionary<string, object>
                {
                    ["Path"] = path,
                    ["Method"] = method,
                    ["Pattern"] = "DirectoryTraversal"
                });
        }

        // Check response status codes for potential attacks
        if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
        {
            // This will be handled by specific auth failure logging
            return;
        }

        if (context.Response.StatusCode >= 500)
        {
            _securityLogger.LogSuspiciousActivity(
                "ServerError",
                $"Server error occurred for request to {path}",
                ipAddress,
                username,
                30,
                new Dictionary<string, object>
                {
                    ["Path"] = path,
                    ["Method"] = method,
                    ["StatusCode"] = context.Response.StatusCode,
                    ["Duration"] = duration
                });
        }
    }

    /// <summary>
    /// Checks for SQL injection patterns
    /// </summary>
    /// <param name="input">Input to check</param>
    /// <returns>True if SQL injection patterns detected</returns>
    private static bool HasSqlInjectionPatterns(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var sqlPatterns = new[]
        {
            "union select", "drop table", "insert into", "delete from",
            "exec sp_", "sp_executesql", "xp_cmdshell", "'; --",
            "' or 1=1", "' or '1'='1", "\" or 1=1", "\" or \"1\"=\"1"
        };

        return sqlPatterns.Any(pattern =>
            input.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Checks for XSS patterns in the request
    /// </summary>
    /// <param name="request">HTTP request</param>
    /// <returns>True if XSS patterns detected</returns>
    private static bool HasXssPatterns(HttpRequest request)
    {
        var xssPatterns = new[]
        {
            "<script", "javascript:", "onload=", "onerror=", "onclick=",
            "eval(", "document.cookie", "window.location", "<iframe"
        };

        // Check query string
        var queryString = request.QueryString.Value;
        if (!string.IsNullOrEmpty(queryString))
        {
            if (xssPatterns.Any(pattern =>
                queryString.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        // Check headers
        foreach (var header in request.Headers)
        {
            var headerValue = string.Join(" ", header.Value.ToArray());
            if (xssPatterns.Any(pattern =>
                headerValue.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks for directory traversal patterns
    /// </summary>
    /// <param name="path">Path to check</param>
    /// <returns>True if directory traversal patterns detected</returns>
    private static bool HasDirectoryTraversalPatterns(string path)
    {
        var patterns = new[] { "../", "..\\", "%2e%2e%2f", "%2e%2e%5c", "..%2f", "..%5c" };
        return patterns.Any(pattern =>
            path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Extracts required role from authorization attributes
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Required role or "Unknown"</returns>
    private static string ExtractRequiredRole(HttpContext context)
    {
        // This would typically extract from route metadata or authorization policies
        // For now, return a generic value
        return "Authenticated";
    }

    /// <summary>
    /// Extracts user role from claims
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>User role or null</returns>
    private static string? ExtractUserRole(HttpContext context)
    {
        return context.User?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Sanitizes query string for logging
    /// </summary>
    /// <param name="queryString">Query string to sanitize</param>
    /// <returns>Sanitized query string</returns>
    private static string? SanitizeQueryString(string? queryString)
    {
        if (string.IsNullOrEmpty(queryString))
            return null;

        // Truncate long query strings and remove sensitive patterns
        var sanitized = queryString.Length > 200 ? queryString[..200] + "..." : queryString;

        // Remove potential passwords or tokens
        sanitized = System.Text.RegularExpressions.Regex.Replace(
            sanitized,
            @"(password|token|key|secret)=[^&]*",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return sanitized;
    }

    /// <summary>
    /// Sanitizes exception for safe logging by removing sensitive details
    /// </summary>
    /// <param name="exception">Exception to sanitize</param>
    /// <returns>Sanitized exception</returns>
    private static Exception SanitizeException(Exception exception)
    {
        try
        {
            // Create a new exception with sanitized message
            var sanitizedMessage = SanitizeExceptionMessage(exception.Message);

            return exception switch
            {
                ArgumentException => new ArgumentException(sanitizedMessage),
                InvalidOperationException => new InvalidOperationException(sanitizedMessage),
                UnauthorizedAccessException => new UnauthorizedAccessException("Access denied"),
                SecurityException => new SecurityException(sanitizedMessage),
                _ => new Exception(sanitizedMessage)
            };
        }
        catch
        {
            // If sanitization fails, return generic exception
            return new Exception("An error occurred during request processing");
        }
    }

    /// <summary>
    /// Sanitizes exception message for logging
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <returns>Sanitized message</returns>
    private static string SanitizeExceptionMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return "Unknown error";

        // Remove file paths that might contain sensitive information
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"[a-zA-Z]:\\[^:\s]*|\/[^:\s]*",
            "[FILE_PATH]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove potential connection strings
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(server|host|database|uid|pwd|password|token|key|secret)\s*=\s*[^;\s]*",
            "$1=[REDACTED]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove SQL query details that might contain sensitive data
        message = System.Text.RegularExpressions.Regex.Replace(
            message,
            @"(select|insert|update|delete|from|where|values)\s+[^.]*",
            "[SQL_QUERY]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Truncate very long messages
        if (message.Length > 500)
        {
            message = message[..500] + "...";
        }

        return message;
    }

    /// <summary>
    /// Sanitizes exception details for logging (removes stack traces and sensitive info)
    /// </summary>
    /// <param name="exception">Exception to sanitize</param>
    /// <returns>Sanitized exception details</returns>
    private static string SanitizeExceptionDetails(Exception exception)
    {
        // Don't include full stack traces in logs - security risk
        var details = new StringBuilder();

        details.AppendLine($"Exception Type: {exception.GetType().Name}");
        details.AppendLine($"Message: {SanitizeExceptionMessage(exception.Message)}");

        // Include inner exception type but not details
        if (exception.InnerException != null)
        {
            details.AppendLine($"Inner Exception: {exception.InnerException.GetType().Name}");
        }

        // Include only the immediate source without full stack trace
        if (!string.IsNullOrEmpty(exception.Source))
        {
            details.AppendLine($"Source: {exception.Source}");
        }

        return details.ToString();
    }
}

/// <summary>
/// Security exception for policy violations
/// </summary>
public class SecurityException : Exception
{
    public SecurityException(string message) : base(message) { }
    public SecurityException(string message, Exception innerException) : base(message, innerException) { }
}
