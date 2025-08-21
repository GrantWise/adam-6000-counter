using System.Text.Json;

namespace Industrial.Adam.Security.Models;

/// <summary>
/// Represents a security event in the system
/// </summary>
public class SecurityEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Correlation ID for tracking related events
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the event occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Type of security event
    /// </summary>
    public SecurityEventType EventType { get; set; }

    /// <summary>
    /// Severity level of the event
    /// </summary>
    public SecurityEventSeverity Severity { get; set; }

    /// <summary>
    /// Username associated with the event (if applicable)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// IP address where the event originated
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string from the request
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Resource or endpoint accessed
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// HTTP method used
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Status code or result
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// Event description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Exception details if applicable
    /// </summary>
    public string? ExceptionDetails { get; set; }

    /// <summary>
    /// Session ID if applicable
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Risk score (0-100)
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Converts the security event to a structured log format
    /// </summary>
    /// <returns>JSON representation of the event</returns>
    public string ToJson()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Creates a security event from an authentication attempt
    /// </summary>
    /// <param name="username">Username attempting authentication</param>
    /// <param name="success">Whether authentication succeeded</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="correlationId">Correlation ID</param>
    /// <returns>Security event</returns>
    public static SecurityEvent CreateAuthenticationEvent(
        string username,
        bool success,
        string? ipAddress,
        string? userAgent,
        string correlationId)
    {
        return new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = success ? SecurityEventType.AuthenticationSuccess : SecurityEventType.AuthenticationFailure,
            Severity = success ? SecurityEventSeverity.Information : SecurityEventSeverity.Warning,
            Username = username,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = success ?
                $"User '{username}' authenticated successfully" :
                $"Authentication failed for user '{username}'",
            RiskScore = success ? 0 : 25,
            Metadata = new Dictionary<string, object>
            {
                ["AuthenticationMethod"] = "JWT",
                ["Success"] = success
            }
        };
    }

    /// <summary>
    /// Creates a security event from an authorization failure
    /// </summary>
    /// <param name="username">Username attempting access</param>
    /// <param name="resource">Resource being accessed</param>
    /// <param name="requiredRole">Required role for access</param>
    /// <param name="userRole">User's actual role</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="correlationId">Correlation ID</param>
    /// <returns>Security event</returns>
    public static SecurityEvent CreateAuthorizationFailureEvent(
        string? username,
        string resource,
        string requiredRole,
        string? userRole,
        string? ipAddress,
        string correlationId)
    {
        return new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.AuthorizationFailure,
            Severity = SecurityEventSeverity.Warning,
            Username = username,
            IpAddress = ipAddress,
            Resource = resource,
            Description = $"Access denied to '{resource}'. Required role: '{requiredRole}', User role: '{userRole}'",
            RiskScore = 30,
            Metadata = new Dictionary<string, object>
            {
                ["RequiredRole"] = requiredRole,
                ["UserRole"] = userRole ?? "None",
                ["AccessType"] = "Authorization"
            }
        };
    }

    /// <summary>
    /// Creates a security event from suspicious activity
    /// </summary>
    /// <param name="activityType">Type of suspicious activity</param>
    /// <param name="description">Description of the activity</param>
    /// <param name="ipAddress">IP address involved</param>
    /// <param name="correlationId">Correlation ID</param>
    /// <param name="riskScore">Risk score (0-100)</param>
    /// <returns>Security event</returns>
    public static SecurityEvent CreateSuspiciousActivityEvent(
        string activityType,
        string description,
        string? ipAddress,
        string correlationId,
        int riskScore = 50)
    {
        return new SecurityEvent
        {
            CorrelationId = correlationId,
            EventType = SecurityEventType.SuspiciousActivity,
            Severity = riskScore > 70 ? SecurityEventSeverity.High : SecurityEventSeverity.Medium,
            IpAddress = ipAddress,
            Description = description,
            RiskScore = riskScore,
            Metadata = new Dictionary<string, object>
            {
                ["ActivityType"] = activityType,
                ["DetectionMethod"] = "Automated"
            }
        };
    }
}

/// <summary>
/// Types of security events
/// </summary>
public enum SecurityEventType
{
    /// <summary>
    /// Successful authentication
    /// </summary>
    AuthenticationSuccess,

    /// <summary>
    /// Failed authentication attempt
    /// </summary>
    AuthenticationFailure,

    /// <summary>
    /// Authorization failure
    /// </summary>
    AuthorizationFailure,

    /// <summary>
    /// Input validation failure
    /// </summary>
    ValidationFailure,

    /// <summary>
    /// Suspicious activity detected
    /// </summary>
    SuspiciousActivity,

    /// <summary>
    /// Configuration change
    /// </summary>
    ConfigurationChange,

    /// <summary>
    /// Data access event
    /// </summary>
    DataAccess,

    /// <summary>
    /// Security policy violation
    /// </summary>
    PolicyViolation,

    /// <summary>
    /// System security alert
    /// </summary>
    SecurityAlert
}

/// <summary>
/// Security event severity levels
/// </summary>
public enum SecurityEventSeverity
{
    /// <summary>
    /// Informational event
    /// </summary>
    Information,

    /// <summary>
    /// Low severity event
    /// </summary>
    Low,

    /// <summary>
    /// Warning event
    /// </summary>
    Warning,

    /// <summary>
    /// Medium severity event
    /// </summary>
    Medium,

    /// <summary>
    /// High severity event
    /// </summary>
    High,

    /// <summary>
    /// Critical security event
    /// </summary>
    Critical
}
