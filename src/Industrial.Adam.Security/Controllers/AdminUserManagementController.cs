using Industrial.Adam.Security.Domain.Constants;
using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Infrastructure.Services;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Industrial.Adam.Security.Controllers;

/// <summary>
/// Enhanced user management controller for admin dashboard Phase 2.1
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "RequireSystemAdmin")]
[EnableRateLimiting("ApiPolicy")] // Apply API-specific rate limiting to entire controller
public class AdminUserManagementController : ControllerBase
{
    private readonly EnhancedUserManagementService _userManagementService;
    private readonly ILogger<AdminUserManagementController> _logger;

    public AdminUserManagementController(
        EnhancedUserManagementService userManagementService,
        ILogger<AdminUserManagementController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets active sessions for a specific user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User's active sessions</returns>
    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<UserSessionsResponse>> GetUserSessions(
        [FromRoute] int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var sessions = await _userManagementService.GetUserSessionsAsync(id, cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get sessions for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve user sessions" });
        }
    }

    /// <summary>
    /// Gets activity history for a specific user
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>User's activity history</returns>
    [HttpGet("{id}/activity")]
    public async Task<ActionResult<UserActivityResponse>> GetUserActivity(
        [FromRoute] int id,
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

            var activity = await _userManagementService.GetUserActivityAsync(id, page, pageSize, cancellationToken);
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activity for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve user activity" });
        }
    }

    /// <summary>
    /// Gets login history for a specific user
    /// </summary>
    /// <param name="id">User ID or username</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50)</param>
    /// <returns>User's login history</returns>
    [HttpGet("{id}/login-history")]
    public async Task<ActionResult<LoginHistoryResponse>> GetUserLoginHistory(
        [FromRoute] string id,
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

            var loginHistory = await _userManagementService.GetUserLoginHistoryAsync(id, page, pageSize, cancellationToken);
            return Ok(loginHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get login history for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve login history" });
        }
    }

    /// <summary>
    /// Performs bulk password reset for multiple users
    /// </summary>
    /// <param name="request">Bulk password reset request</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("bulk/reset")]
    public async Task<ActionResult<BulkOperationResult>> BulkPasswordReset(
        [FromBody] BulkPasswordResetRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.UserIds?.Length == 0)
            {
                return BadRequest(new { error = "No users specified for password reset" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var result = await _userManagementService.BulkPasswordResetAsync(request, User, ipAddress, cancellationToken);

            _logger.LogInformation("Bulk password reset completed: {SuccessCount}/{TotalCount} successful", 
                result.SuccessCount, result.TotalCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform bulk password reset");
            return StatusCode(500, new { error = "Bulk password reset failed" });
        }
    }

    /// <summary>
    /// Locks or unlocks a user account
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Lock user request</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/lock")]
    public async Task<ActionResult> LockUserAccount(
        [FromRoute] string id,
        [FromBody] LockUserRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Reason))
            {
                return BadRequest(new { error = "Lock reason is required" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _userManagementService.LockUserAccountAsync(id, request, User, ipAddress, cancellationToken);

            if (success)
            {
                return Ok(new { message = "User account locked successfully" });
            }

            return StatusCode(500, new { error = "Failed to lock user account" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock user {UserId}", id);
            return StatusCode(500, new { error = "Failed to lock user account" });
        }
    }

    /// <summary>
    /// Terminates user sessions
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="request">Session termination request</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/sessions")]
    public async Task<ActionResult> TerminateUserSessions(
        [FromRoute] int id,
        [FromBody] TerminateSessionsRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _userManagementService.TerminateUserSessionsAsync(
                id, 
                request?.SessionTokens, 
                User, 
                ipAddress, 
                cancellationToken);

            if (success)
            {
                var sessionCount = request?.SessionTokens?.Length ?? 0;
                var message = sessionCount > 0 
                    ? $"Terminated {sessionCount} specific sessions" 
                    : "Terminated all active sessions";
                    
                return Ok(new { message });
            }

            return StatusCode(500, new { error = "Failed to terminate sessions" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to terminate sessions for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to terminate sessions" });
        }
    }

    /// <summary>
    /// Gets role permissions matrix
    /// </summary>
    /// <returns>Role permissions matrix</returns>
    [HttpGet("roles/permissions")]
    public ActionResult<RolePermissionsMatrix> GetRolePermissions()
    {
        try
        {
            // This would typically be retrieved from a database
            // For now, return a static configuration based on RoleConstants
            var matrix = new RolePermissionsMatrix
            {
                Roles = new List<RolePermissions>
                {
                    new() 
                    { 
                        RoleName = RoleConstants.SystemAdmin,
                        Permissions = new[]
                        {
                            "system.admin", "user.manage", "config.modify", "audit.view", 
                            "security.manage", "logs.view", "maintenance.schedule"
                        }
                    },
                    new() 
                    { 
                        RoleName = RoleConstants.Admin,
                        Permissions = new[]
                        {
                            "user.manage", "audit.view", "logs.view", "reports.generate"
                        }
                    },
                    new() 
                    { 
                        RoleName = RoleConstants.Supervisor,
                        Permissions = new[]
                        {
                            "production.monitor", "reports.view", "user.view"
                        }
                    },
                    new() 
                    { 
                        RoleName = RoleConstants.Operator,
                        Permissions = new[]
                        {
                            "production.operate", "data.view"
                        }
                    }
                }
            };

            return Ok(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get role permissions");
            return StatusCode(500, new { error = "Failed to retrieve role permissions" });
        }
    }
}

/// <summary>
/// Request for terminating user sessions
/// </summary>
public class TerminateSessionsRequest
{
    /// <summary>
    /// Specific session tokens to terminate (if empty, terminates all)
    /// </summary>
    public string[]? SessionTokens { get; set; }
}

/// <summary>
/// Role permissions matrix response
/// </summary>
public class RolePermissionsMatrix
{
    public List<RolePermissions> Roles { get; set; } = new();
}

/// <summary>
/// Role with its permissions
/// </summary>
public class RolePermissions
{
    public string RoleName { get; set; } = string.Empty;
    public string[] Permissions { get; set; } = [];
    public string Description { get; set; } = string.Empty;
}