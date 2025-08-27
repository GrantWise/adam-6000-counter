using System.Diagnostics;
using System.Security.Claims;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Infrastructure.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Infrastructure.Middleware;

/// <summary>
/// Security-focused middleware that integrates with native ASP.NET Core rate limiting
/// to provide comprehensive security event logging and enhanced monitoring for compliance
/// </summary>
public class SecurityRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityRateLimitingMiddleware> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly Dictionary<string, DateTime> _recentViolations = new();
    private readonly object _violationsLock = new();

    public SecurityRateLimitingMiddleware(
        RequestDelegate next,
        ILogger<SecurityRateLimitingMiddleware> logger,
        SecurityEventLogger securityLogger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _securityLogger = securityLogger ?? throw new ArgumentNullException(nameof(securityLogger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Store request info for potential logging
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        var policyName = DeterminePolicyName(path);
        var clientInfo = ExtractClientInfo(context);
        
        context.Items["RateLimitPolicy"] = policyName;
        context.Items["ClientInfo"] = clientInfo;
        context.Items["RequestStartTime"] = startTime;

        // Execute the next middleware
        await _next(context);

        // Check if rate limiting was applied and rejected
        if (context.Items.ContainsKey("RateLimitRejected"))
        {
            await LogRateLimitEvent(context);
            await CheckForSuspiciousPatterns(context);
        }
    }

    /// <summary>
    /// Determines the appropriate rate limiting policy based on the request path
    /// This is used for logging purposes - the actual policy is applied via controller attributes
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>Policy name</returns>
    private string DeterminePolicyName(string path)
    {
        // Authentication endpoints get the most restrictive policy
        if (IsAuthenticationEndpoint(path))
        {
            return "AuthPolicy";
        }

        // API endpoints get API-specific policy
        if (IsApiEndpoint(path))
        {
            return "ApiPolicy";
        }

        // Everything else gets global policy
        return "GlobalPolicy";
    }

    /// <summary>
    /// Logs comprehensive security event when rate limiting is triggered
    /// </summary>
    /// <param name="context">HTTP context</param>
    private Task LogRateLimitEvent(HttpContext context)
    {
        var clientInfo = (ClientInfo?)context.Items["ClientInfo"] ?? ExtractClientInfo(context);
        var policyName = context.Items["RateLimitPolicy"]?.ToString() ?? "Unknown";
        var startTime = (DateTimeOffset?)context.Items["RequestStartTime"] ?? DateTimeOffset.UtcNow;

        // Create comprehensive security event for rate limit exceeded
        var securityEvent = SecurityEvent.CreateSuspiciousActivityEvent(
            "Rate limit exceeded - Potential security threat",
            clientInfo.IpAddress,
            clientInfo.UserAgent,
            Activity.Current?.Id ?? Guid.NewGuid().ToString());

        securityEvent.Username = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        securityEvent.Resource = context.Request.Path.Value ?? "";
        securityEvent.Metadata["request_method"] = context.Request.Method;
        securityEvent.Metadata["client_id"] = clientInfo.ClientId;
        securityEvent.Metadata["endpoint"] = context.Request.Path.Value ?? "";
        securityEvent.Metadata["policy_applied"] = policyName;
        securityEvent.Metadata["user_agent"] = clientInfo.UserAgent;
        securityEvent.Metadata["client_ip"] = clientInfo.IpAddress;
        securityEvent.Metadata["forwarded_for"] = clientInfo.ForwardedFor;
        securityEvent.Metadata["request_time"] = startTime.ToString("O");
        securityEvent.Metadata["response_status"] = context.Response.StatusCode.ToString();
        securityEvent.Metadata["content_type"] = context.Request.ContentType ?? "";
        securityEvent.Metadata["content_length"] = context.Request.ContentLength?.ToString() ?? "0";
        securityEvent.Metadata["referer"] = context.Request.Headers.Referer.ToString();
        securityEvent.Metadata["origin"] = context.Request.Headers.Origin.ToString();
        
        // Add severity assessment
        var severity = AssessRateLimitViolationSeverity(policyName, clientInfo.IpAddress);
        securityEvent.Metadata["violation_severity"] = severity.ToString();
        
        // Add CFR Part 11 compliance tracking
        securityEvent.Metadata["compliance_event"] = "true";
        securityEvent.Metadata["audit_category"] = "RATE_LIMIT_VIOLATION";
        securityEvent.Metadata["requires_review"] = (severity == ViolationSeverity.High).ToString();

        _securityLogger.LogSecurityEvent(securityEvent);

        // Enhanced structured logging with security context
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = Activity.Current?.Id ?? "",
            ["ClientIP"] = clientInfo.IpAddress,
            ["PolicyName"] = policyName,
            ["ViolationSeverity"] = severity.ToString(),
            ["Username"] = securityEvent.Username ?? "anonymous"
        }))
        {
            _logger.LogWarning(
                "Rate limit violation detected: Policy={PolicyName}, IP={IpAddress}, User={Username}, Endpoint={Endpoint}, Severity={Severity}, Method={Method}",
                policyName, clientInfo.IpAddress, securityEvent.Username ?? "anonymous", 
                securityEvent.Resource, severity, context.Request.Method);
        }
            
        return Task.CompletedTask;
    }


    /// <summary>
    /// Determines if the path is an authentication endpoint
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if authentication endpoint</returns>
    private bool IsAuthenticationEndpoint(string path)
    {
        var authPaths = new[]
        {
            "/api/auth/login",
            "/api/auth/token", 
            "/api/auth/refresh",
            "/login",
            "/signin",
            "/auth/"
        };

        return authPaths.Any(authPath => path.Contains(authPath, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Determines if the path is an API endpoint
    /// </summary>
    /// <param name="path">Request path</param>
    /// <returns>True if API endpoint</returns>
    private bool IsApiEndpoint(string path)
    {
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks for suspicious patterns that might indicate coordinated attacks
    /// </summary>
    /// <param name="context">HTTP context</param>
    private Task CheckForSuspiciousPatterns(HttpContext context)
    {
        var clientInfo = (ClientInfo?)context.Items["ClientInfo"] ?? ExtractClientInfo(context);
        var policyName = context.Items["RateLimitPolicy"]?.ToString() ?? "Unknown";
        
        lock (_violationsLock)
        {
            // Clean old violations (older than 1 hour)
            var expiredKeys = _recentViolations
                .Where(kvp => DateTime.UtcNow.Subtract(kvp.Value).TotalHours > 1)
                .Select(kvp => kvp.Key)
                .ToList();
                
            foreach (var key in expiredKeys)
            {
                _recentViolations.Remove(key);
            }
            
            // Track current violation
            var violationKey = $"{clientInfo.IpAddress}:{policyName}";
            _recentViolations[violationKey] = DateTime.UtcNow;
            
            // Check for repeated violations from same IP
            var ipViolations = _recentViolations
                .Where(kvp => kvp.Key.StartsWith($"{clientInfo.IpAddress}:"))
                .Count();
                
            if (ipViolations > 3) // More than 3 violations from same IP in the last hour
            {
                var alertEvent = SecurityEvent.CreateSuspiciousActivityEvent(
                    "Repeated rate limit violations - Potential coordinated attack",
                    clientInfo.IpAddress,
                    clientInfo.UserAgent,
                    Activity.Current?.Id ?? Guid.NewGuid().ToString());
                    
                alertEvent.Metadata["violation_count"] = ipViolations.ToString();
                alertEvent.Metadata["threat_level"] = "HIGH";
                alertEvent.Metadata["recommended_action"] = "CONSIDER_IP_BLOCKING";
                alertEvent.Metadata["compliance_alert"] = "true";
                
                _securityLogger.LogSecurityEvent(alertEvent);
                
                _logger.LogError(
                    "SECURITY ALERT: Repeated rate limit violations from IP {IpAddress}. Count: {ViolationCount}. Consider blocking.",
                    clientInfo.IpAddress, ipViolations);
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Assesses the severity of a rate limit violation
    /// </summary>
    /// <param name="policyName">Applied rate limit policy</param>
    /// <param name="ipAddress">Client IP address</param>
    /// <returns>Violation severity</returns>
    private ViolationSeverity AssessRateLimitViolationSeverity(string policyName, string ipAddress)
    {
        // Authentication endpoints are always high severity
        if (policyName == "AuthPolicy")
        {
            return ViolationSeverity.High;
        }
        
        // Check for repeated violations from same IP
        lock (_violationsLock)
        {
            var ipViolations = _recentViolations
                .Where(kvp => kvp.Key.StartsWith($"{ipAddress}:") && 
                             DateTime.UtcNow.Subtract(kvp.Value).TotalMinutes <= 10)
                .Count();
                
            if (ipViolations > 2)
            {
                return ViolationSeverity.High;
            }
            
            if (ipViolations > 1)
            {
                return ViolationSeverity.Medium;
            }
        }
        
        return ViolationSeverity.Low;
    }

    /// <summary>
    /// Extracts comprehensive client information from HTTP context
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Client information</returns>
    private ClientInfo ExtractClientInfo(HttpContext context)
    {
        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var forwardedFor = string.Empty;
        var actualIp = remoteIp;

        // Check for forwarded IP (behind proxy/load balancer)
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedForHeader))
        {
            forwardedFor = forwardedForHeader.ToString();
            actualIp = forwardedFor.Split(',').FirstOrDefault()?.Trim() ?? remoteIp;
        }

        // Also check X-Real-IP header
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIpHeader))
        {
            var realIp = realIpHeader.ToString().Trim();
            if (!string.IsNullOrEmpty(realIp))
            {
                actualIp = realIp;
            }
        }

        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var clientId = !string.IsNullOrEmpty(userId) ? $"user:{userId}" : $"ip:{actualIp}";

        return new ClientInfo
        {
            IpAddress = actualIp,
            RemoteIpAddress = remoteIp,
            ForwardedFor = forwardedFor,
            UserAgent = userAgent,
            ClientId = clientId,
            UserId = userId
        };
    }

    /// <summary>
    /// Client information model for enhanced tracking
    /// </summary>
    private record ClientInfo
    {
        public string IpAddress { get; init; } = string.Empty;
        public string RemoteIpAddress { get; init; } = string.Empty;
        public string ForwardedFor { get; init; } = string.Empty;
        public string UserAgent { get; init; } = string.Empty;
        public string ClientId { get; init; } = string.Empty;
        public string? UserId { get; init; }
    }

    /// <summary>
    /// Rate limit violation severity levels
    /// </summary>
    private enum ViolationSeverity
    {
        Low,
        Medium,
        High
    }
}