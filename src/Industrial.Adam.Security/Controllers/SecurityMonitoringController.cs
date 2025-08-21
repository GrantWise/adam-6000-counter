using Industrial.Adam.Security.Authorization;
using Industrial.Adam.Security.Logging;
using Industrial.Adam.Security.Models;
using Industrial.Adam.Security.Monitoring;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Controllers;

/// <summary>
/// Controller for security monitoring and metrics endpoints
/// </summary>
[ApiController]
[Route("api/security")]
[Authorize(Policy = "RequireAdmin")]
public class SecurityMonitoringController : ControllerBase
{
    private readonly ILogger<SecurityMonitoringController> _logger;
    private readonly SecurityEventLogger _securityLogger;
    private readonly SecurityMonitoringService _monitoringService;

    public SecurityMonitoringController(
        ILogger<SecurityMonitoringController> logger,
        SecurityEventLogger securityLogger,
        SecurityMonitoringService monitoringService)
    {
        _logger = logger;
        _securityLogger = securityLogger;
        _monitoringService = monitoringService;
    }

    /// <summary>
    /// Gets current security status and health metrics
    /// </summary>
    /// <returns>Security status</returns>
    [HttpGet("status")]
    public ActionResult<SecurityStatus> GetSecurityStatus()
    {
        try
        {
            var status = _monitoringService.GetSecurityStatus();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets security metrics for a specific time period
    /// </summary>
    /// <param name="startTime">Start time for metrics (optional, defaults to 1 hour ago)</param>
    /// <param name="endTime">End time for metrics (optional, defaults to now)</param>
    /// <returns>Security metrics</returns>
    [HttpGet("metrics")]
    public ActionResult<SecurityMetrics> GetSecurityMetrics(
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null)
    {
        try
        {
            var metrics = _securityLogger.GetMetrics(startTime, endTime);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security metrics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets security health check information
    /// </summary>
    /// <returns>Security health check result</returns>
    [HttpGet("health")]
    [AllowAnonymous]
    public ActionResult<SecurityHealthCheck> GetSecurityHealth()
    {
        try
        {
            var metrics = _securityLogger.CurrentMetrics;
            var status = _monitoringService.GetSecurityStatus();

            var healthCheck = new SecurityHealthCheck
            {
                Status = status.HealthStatus.ToString(),
                HealthScore = status.HealthScore,
                ThreatLevel = status.ThreatLevel.ToString(),
                MonitoringActive = status.MonitoringActive,
                LastUpdated = status.LastUpdated,
                Details = new Dictionary<string, object>
                {
                    ["AuthenticationSuccessRate"] = $"{metrics.AuthenticationSuccessRate:F1}%",
                    ["ActiveAlerts"] = status.ActiveAlerts.Count,
                    ["HighRiskEvents"] = metrics.HighRiskEvents,
                    ["CriticalEvents"] = metrics.CriticalEvents
                }
            };

            // Return appropriate HTTP status based on health
            return status.HealthStatus switch
            {
                SecurityHealthStatus.Excellent or SecurityHealthStatus.Good => Ok(healthCheck),
                SecurityHealthStatus.Warning => StatusCode(200, healthCheck), // Warning but still OK
                SecurityHealthStatus.Poor => StatusCode(503, healthCheck), // Service degraded
                SecurityHealthStatus.Critical => StatusCode(503, healthCheck), // Service unavailable
                _ => Ok(healthCheck)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing security health check");
            return StatusCode(500, new SecurityHealthCheck
            {
                Status = "Error",
                HealthScore = 0,
                MonitoringActive = false,
                LastUpdated = DateTimeOffset.UtcNow,
                Details = new Dictionary<string, object>
                {
                    ["Error"] = "Health check failed"
                }
            });
        }
    }

    /// <summary>
    /// Gets active security alerts
    /// </summary>
    /// <returns>List of active alerts</returns>
    [HttpGet("alerts")]
    public ActionResult<List<SecurityAlert>> GetActiveAlerts()
    {
        try
        {
            var status = _monitoringService.GetSecurityStatus();
            return Ok(status.ActiveAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active alerts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets security events for analysis
    /// </summary>
    /// <param name="eventType">Filter by event type (optional)</param>
    /// <param name="severity">Filter by severity (optional)</param>
    /// <param name="startTime">Start time for events (optional, defaults to 1 hour ago)</param>
    /// <param name="endTime">End time for events (optional, defaults to now)</param>
    /// <param name="limit">Maximum number of events to return (default 100)</param>
    /// <returns>List of security events</returns>
    [HttpGet("events")]
    [Authorize(Policy = "RequireSystemAdmin")]
    public ActionResult<SecurityEventResponse> GetSecurityEvents(
        [FromQuery] SecurityEventType? eventType = null,
        [FromQuery] SecurityEventSeverity? severity = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] int limit = 100)
    {
        try
        {
            // This would typically query from a persistent store
            // For now, return metrics with filtered information
            var metrics = _securityLogger.GetMetrics(
                startTime ?? DateTimeOffset.UtcNow.AddHours(-1),
                endTime ?? DateTimeOffset.UtcNow);

            var response = new SecurityEventResponse
            {
                TotalEvents = (int)(metrics.AuthenticationAttempts + metrics.ValidationFailures +
                                   metrics.SuspiciousActivities + metrics.AuthorizationFailures),
                FilteredEvents = Math.Min(limit, 50), // Placeholder
                StartTime = startTime ?? DateTimeOffset.UtcNow.AddHours(-1),
                EndTime = endTime ?? DateTimeOffset.UtcNow,
                EventSummary = metrics.EventsByType,
                SeveritySummary = metrics.EventsBySeverity
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets security dashboard data
    /// </summary>
    /// <returns>Dashboard data</returns>
    [HttpGet("dashboard")]
    public ActionResult<SecurityDashboard> GetSecurityDashboard()
    {
        try
        {
            var metrics = _securityLogger.CurrentMetrics;
            var status = _monitoringService.GetSecurityStatus();

            var dashboard = new SecurityDashboard
            {
                OverallHealth = new HealthSummary
                {
                    Status = status.HealthStatus.ToString(),
                    Score = status.HealthScore,
                    ThreatLevel = status.ThreatLevel.ToString()
                },
                AuthenticationMetrics = new AuthenticationSummary
                {
                    TotalAttempts = metrics.AuthenticationAttempts,
                    SuccessfulAttempts = metrics.AuthenticationSuccesses,
                    FailedAttempts = metrics.AuthenticationFailures,
                    SuccessRate = metrics.AuthenticationSuccessRate
                },
                ThreatDetection = new ThreatDetectionSummary
                {
                    SuspiciousActivities = metrics.SuspiciousActivities,
                    HighRiskEvents = metrics.HighRiskEvents,
                    CriticalEvents = metrics.CriticalEvents,
                    ActiveAlerts = status.ActiveAlerts.Count
                },
                TopThreats = new TopThreats
                {
                    IpAddresses = metrics.TopIpAddresses
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(10)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    Users = metrics.TopUsers
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(10)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                },
                RecentAlerts = status.ActiveAlerts
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .ToList(),
                LastUpdated = DateTimeOffset.UtcNow
            };

            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating security dashboard");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Logs a manual security event (for testing or manual reporting)
    /// </summary>
    /// <param name="request">Manual security event request</param>
    /// <returns>Result</returns>
    [HttpPost("events")]
    [Authorize(Policy = "RequireSystemAdmin")]
    public ActionResult LogSecurityEvent([FromBody] ManualSecurityEventRequest request)
    {
        try
        {
            var username = User.Identity?.Name ?? "System";
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            _securityLogger.LogSuspiciousActivity(
                request.ActivityType,
                request.Description,
                ipAddress,
                username,
                request.RiskScore,
                request.Metadata);

            // Also log the manual reporting action
            _securityLogger.LogConfigurationChange(
                "ManualSecurityEvent",
                null,
                request.Description,
                username,
                ipAddress);

            return Ok(new { message = "Security event logged successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging manual security event");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Security health check response
/// </summary>
public class SecurityHealthCheck
{
    public string Status { get; set; } = string.Empty;
    public int HealthScore { get; set; }
    public string ThreatLevel { get; set; } = string.Empty;
    public bool MonitoringActive { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Security event query response
/// </summary>
public class SecurityEventResponse
{
    public int TotalEvents { get; set; }
    public int FilteredEvents { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public Dictionary<SecurityEventType, long> EventSummary { get; set; } = new();
    public Dictionary<SecurityEventSeverity, long> SeveritySummary { get; set; } = new();
}

/// <summary>
/// Security dashboard data
/// </summary>
public class SecurityDashboard
{
    public HealthSummary OverallHealth { get; set; } = new();
    public AuthenticationSummary AuthenticationMetrics { get; set; } = new();
    public ThreatDetectionSummary ThreatDetection { get; set; } = new();
    public TopThreats TopThreats { get; set; } = new();
    public List<SecurityAlert> RecentAlerts { get; set; } = new();
    public DateTimeOffset LastUpdated { get; set; }
}

public class HealthSummary
{
    public string Status { get; set; } = string.Empty;
    public int Score { get; set; }
    public string ThreatLevel { get; set; } = string.Empty;
}

public class AuthenticationSummary
{
    public long TotalAttempts { get; set; }
    public long SuccessfulAttempts { get; set; }
    public long FailedAttempts { get; set; }
    public double SuccessRate { get; set; }
}

public class ThreatDetectionSummary
{
    public long SuspiciousActivities { get; set; }
    public long HighRiskEvents { get; set; }
    public long CriticalEvents { get; set; }
    public int ActiveAlerts { get; set; }
}

public class TopThreats
{
    public Dictionary<string, long> IpAddresses { get; set; } = new();
    public Dictionary<string, long> Users { get; set; } = new();
}

/// <summary>
/// Request for manually logging a security event
/// </summary>
public class ManualSecurityEventRequest
{
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int RiskScore { get; set; } = 50;
    public Dictionary<string, object> Metadata { get; set; } = new();
}
