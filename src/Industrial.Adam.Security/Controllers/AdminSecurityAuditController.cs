using Industrial.Adam.Security.Application.DTOs;
using Industrial.Adam.Security.Infrastructure.Services;
using Industrial.Adam.Security.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Controllers;

/// <summary>
/// Security audit dashboard controller for admin dashboard Phase 2.2
/// </summary>
[ApiController]
[Route("api/admin/security")]
[Authorize(Policy = "RequireSystemAdmin")]
[EnableRateLimiting("ApiPolicy")]
public class AdminSecurityAuditController : ControllerBase
{
    private readonly SecurityAuditService _securityAuditService;
    private readonly ILogger<AdminSecurityAuditController> _logger;

    public AdminSecurityAuditController(
        SecurityAuditService securityAuditService,
        ILogger<AdminSecurityAuditController> logger)
    {
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets login attempts with filtering and pagination
    /// </summary>
    /// <param name="username">Filter by username (optional)</param>
    /// <param name="success">Filter by success status (optional)</param>
    /// <param name="startTime">Start time for filtering (optional)</param>
    /// <param name="endTime">End time for filtering (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Login attempts history</returns>
    [HttpGet("login-attempts")]
    public async Task<ActionResult<LoginHistoryResponse>> GetLoginAttempts(
        [FromQuery] string? username = null,
        [FromQuery] bool? success = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 1000)
            {
                return BadRequest(new { error = "Invalid pagination parameters" });
            }

            var response = await _securityAuditService.GetLoginAttemptsAsync(
                username, success, startTime, endTime, page, pageSize, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login attempts");
            return StatusCode(500, new { error = "Failed to retrieve login attempts" });
        }
    }

    /// <summary>
    /// Gets complete audit trail with filtering and pagination
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="action">Filter by action (optional)</param>
    /// <param name="entityType">Filter by entity type (optional)</param>
    /// <param name="startTime">Start time for filtering (optional)</param>
    /// <param name="endTime">End time for filtering (optional)</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>Audit trail records</returns>
    [HttpGet("audit-trail")]
    public async Task<ActionResult<AuditTrailResponse>> GetAuditTrail(
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityType = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 1000)
            {
                return BadRequest(new { error = "Invalid pagination parameters" });
            }

            var response = await _securityAuditService.GetAuditTrailAsync(
                userId, action, entityType, startTime, endTime, page, pageSize, cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit trail");
            return StatusCode(500, new { error = "Failed to retrieve audit trail" });
        }
    }

    /// <summary>
    /// Gets user activity across the system
    /// </summary>
    /// <param name="userId">Filter by user ID (optional)</param>
    /// <param name="startTime">Start time for filtering (optional)</param>
    /// <param name="endTime">End time for filtering (optional)</param>
    /// <param name="limit">Maximum number of records (default: 100)</param>
    /// <returns>User activity records</returns>
    [HttpGet("user-activity")]
    public async Task<ActionResult<List<UserActivity>>> GetUserActivity(
        [FromQuery] int? userId = null,
        [FromQuery] DateTimeOffset? startTime = null,
        [FromQuery] DateTimeOffset? endTime = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (limit < 1 || limit > 10000)
            {
                return BadRequest(new { error = "Invalid limit parameter (1-10000)" });
            }

            var activities = await _securityAuditService.GetUserActivityAsync(
                userId, startTime, endTime, limit, cancellationToken);

            return Ok(activities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity");
            return StatusCode(500, new { error = "Failed to retrieve user activity" });
        }
    }

    /// <summary>
    /// Gets active user sessions across the system
    /// </summary>
    /// <returns>Active user sessions</returns>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<UserSession>>> GetActiveSessions(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _securityAuditService.GetActiveSessionsAsync(cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active sessions");
            return StatusCode(500, new { error = "Failed to retrieve active sessions" });
        }
    }

    /// <summary>
    /// Generates CFR Part 11 compliance report
    /// </summary>
    /// <param name="startDate">Report start date</param>
    /// <param name="endDate">Report end date</param>
    /// <returns>Compliance report</returns>
    [HttpGet("compliance-report")]
    public async Task<ActionResult<ComplianceReport>> GenerateComplianceReport(
        [FromQuery] DateTimeOffset startDate,
        [FromQuery] DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (startDate >= endDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            if (endDate.Subtract(startDate).TotalDays > 365)
            {
                return BadRequest(new { error = "Report period cannot exceed 365 days" });
            }

            var report = await _securityAuditService.GenerateComplianceReportAsync(
                startDate, endDate, cancellationToken);

            _logger.LogInformation("Generated compliance report for period {StartDate} to {EndDate}", 
                startDate, endDate);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate compliance report");
            return StatusCode(500, new { error = "Failed to generate compliance report" });
        }
    }

    /// <summary>
    /// Exports audit logs in various formats
    /// </summary>
    /// <param name="request">Export request parameters</param>
    /// <returns>Exported audit data</returns>
    [HttpPost("export-audit")]
    public async Task<ActionResult> ExportAuditLogs(
        [FromBody] AuditExportRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.StartDate >= request.EndDate)
            {
                return BadRequest(new { error = "Start date must be before end date" });
            }

            if (request.EndDate.Subtract(request.StartDate).TotalDays > 365)
            {
                return BadRequest(new { error = "Export period cannot exceed 365 days" });
            }

            var result = await _securityAuditService.ExportAuditLogsAsync(request, cancellationToken);

            _logger.LogInformation("Exported {RecordCount} audit records in {Format} format", 
                result.RecordCount, request.Format);

            return File(
                System.Text.Encoding.UTF8.GetBytes(result.Content),
                result.ContentType,
                result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export audit logs");
            return StatusCode(500, new { error = "Failed to export audit logs" });
        }
    }

    /// <summary>
    /// Gets security dashboard summary data
    /// </summary>
    /// <param name="timeRange">Time range for data (hours, default: 24)</param>
    /// <returns>Security dashboard data</returns>
    [HttpGet("dashboard")]
    public async Task<ActionResult<SecurityDashboardData>> GetSecurityDashboard(
        [FromQuery] int timeRange = 24,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (timeRange < 1 || timeRange > 8760) // Max 1 year
            {
                return BadRequest(new { error = "Invalid time range (1-8760 hours)" });
            }

            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime.AddHours(-timeRange);

            // Get data for dashboard
            var loginAttempts = await _securityAuditService.GetLoginAttemptsAsync(
                null, null, startTime, endTime, 1, 1000, cancellationToken);

            var auditTrail = await _securityAuditService.GetAuditTrailAsync(
                null, null, null, startTime, endTime, 1, 1000, cancellationToken);

            var userActivity = await _securityAuditService.GetUserActivityAsync(
                null, startTime, endTime, 1000, cancellationToken);

            var activeSessions = await _securityAuditService.GetActiveSessionsAsync(cancellationToken);

            var dashboardData = new SecurityDashboardData
            {
                TimeRange = timeRange,
                GeneratedAt = DateTimeOffset.UtcNow,
                Login = new SecurityDashboardData.LoginMetrics
                {
                    TotalAttempts = loginAttempts.TotalCount,
                    SuccessfulAttempts = loginAttempts.SuccessfulLogins,
                    FailedAttempts = loginAttempts.FailedLogins,
                    SuccessRate = loginAttempts.TotalCount > 0 ? 
                        Math.Round((double)loginAttempts.SuccessfulLogins / loginAttempts.TotalCount * 100, 2) : 0
                },
                Audit = new SecurityDashboardData.AuditMetrics
                {
                    TotalEvents = auditTrail.TotalCount,
                    UniqueUsers = auditTrail.Records.Select(r => r.UserId).Distinct().Count(),
                    UniqueActions = auditTrail.Records.Select(r => r.Action).Distinct().Count(),
                    TopActions = auditTrail.Records
                        .GroupBy(r => r.Action)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToDictionary(g => g.Key, g => g.Count())
                },
                Activity = new SecurityDashboardData.ActivityMetrics
                {
                    TotalActivities = userActivity.Count,
                    ActiveUsers = userActivity.Select(a => a.UserId).Distinct().Count(),
                    ActiveSessions = activeSessions.Count
                },
                SecurityAlerts = CalculateSecurityAlerts(loginAttempts, auditTrail, userActivity)
            };

            return Ok(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get security dashboard data");
            return StatusCode(500, new { error = "Failed to retrieve security dashboard data" });
        }
    }

    private List<SecurityAlert> CalculateSecurityAlerts(LoginHistoryResponse loginAttempts, 
        AuditTrailResponse auditTrail, List<UserActivity> userActivity)
    {
        var alerts = new List<SecurityAlert>();

        // Check for excessive failed logins
        var failedLoginsByUser = loginAttempts.LoginAttempts
            .Where(l => !l.Success)
            .GroupBy(l => l.Username)
            .Where(g => g.Count() > 5)
            .ToList();

        foreach (var userFailures in failedLoginsByUser)
        {
            alerts.Add(new SecurityAlert
            {
                Id = Guid.NewGuid().ToString(),
                Type = SecurityAlertType.ExcessiveFailedLogins,
                Severity = SecurityAlertSeverity.High,
                Title = "Excessive Failed Login Attempts",
                Message = $"User {userFailures.Key} has {userFailures.Count()} failed login attempts",
                CreatedAt = userFailures.Max(f => f.AttemptedAt),
                IsAcknowledged = false,
                Metadata = new Dictionary<string, object>
                {
                    ["username"] = userFailures.Key,
                    ["failureCount"] = userFailures.Count()
                }
            });
        }

        // Check for suspicious activities
        var suspiciousActions = auditTrail.Records
            .Where(r => r.Action.Contains("Delete") || r.Action.Contains("Modify"))
            .GroupBy(r => r.UserId)
            .Where(g => g.Count() > 10)
            .ToList();

        foreach (var userActions in suspiciousActions)
        {
            alerts.Add(new SecurityAlert
            {
                Id = Guid.NewGuid().ToString(),
                Type = SecurityAlertType.SuspiciousActivity,
                Severity = SecurityAlertSeverity.Medium,
                Title = "High Volume of Modifications",
                Message = $"User {userActions.Key} performed {userActions.Count()} modification actions",
                CreatedAt = userActions.Max(a => a.CreatedAt),
                IsAcknowledged = false,
                Metadata = new Dictionary<string, object>
                {
                    ["userId"] = userActions.Key,
                    ["actionCount"] = userActions.Count()
                }
            });
        }

        return alerts;
    }
}

/// <summary>
/// Security dashboard data response
/// </summary>
public class SecurityDashboardData
{
    public int TimeRange { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
    public LoginMetrics Login { get; set; } = new();
    public AuditMetrics Audit { get; set; } = new();
    public ActivityMetrics Activity { get; set; } = new();
    public List<SecurityAlert> SecurityAlerts { get; set; } = new();

    public class LoginMetrics
    {
        public int TotalAttempts { get; set; }
        public int SuccessfulAttempts { get; set; }
        public int FailedAttempts { get; set; }
        public double SuccessRate { get; set; }
    }

    public class AuditMetrics
    {
        public int TotalEvents { get; set; }
        public int UniqueUsers { get; set; }
        public int UniqueActions { get; set; }
        public Dictionary<string, int> TopActions { get; set; } = new();
    }

    public class ActivityMetrics
    {
        public int TotalActivities { get; set; }
        public int ActiveUsers { get; set; }
        public int ActiveSessions { get; set; }
    }
}

