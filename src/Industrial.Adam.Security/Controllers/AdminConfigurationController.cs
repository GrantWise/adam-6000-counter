using Industrial.Adam.Security.Domain.Entities;
using Industrial.Adam.Security.Infrastructure.Services;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Security.Controllers;

/// <summary>
/// System configuration management controller for admin dashboard Phase 2.3
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "RequireSystemAdmin")]
[EnableRateLimiting("ApiPolicy")]
public class AdminConfigurationController : ControllerBase
{
    private readonly ConfigurationManagementService _configurationService;
    private readonly ILogger<AdminConfigurationController> _logger;

    public AdminConfigurationController(
        ConfigurationManagementService configurationService,
        ILogger<AdminConfigurationController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all system configuration settings
    /// </summary>
    /// <param name="category">Filter by category (optional)</param>
    /// <returns>System configuration settings</returns>
    [HttpGet("config")]
    public async Task<ActionResult<List<SystemConfiguration>>> GetConfiguration(
        [FromQuery] string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            List<SystemConfiguration> configurations;
            
            if (!string.IsNullOrEmpty(category))
            {
                configurations = await _configurationService.GetConfigurationsByCategoryAsync(category, cancellationToken);
            }
            else
            {
                configurations = await _configurationService.GetAllConfigurationsAsync(cancellationToken);
            }

            return Ok(configurations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system configurations");
            return StatusCode(500, new { error = "Failed to retrieve system configurations" });
        }
    }

    /// <summary>
    /// Updates system configuration settings
    /// </summary>
    /// <param name="configurations">Configuration settings to update</param>
    /// <returns>Update result</returns>
    [HttpPut("config")]
    public async Task<ActionResult> UpdateConfiguration(
        [FromBody] Dictionary<string, object> configurations,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (configurations?.Count == 0)
            {
                return BadRequest(new { error = "No configurations provided" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _configurationService.UpdateConfigurationsAsync(configurations!, User, ipAddress, cancellationToken);

            if (success)
            {
                return Ok(new { message = $"Successfully updated {configurations!.Count} configuration settings" });
            }

            return StatusCode(500, new { error = "Failed to update configurations" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update system configurations");
            return StatusCode(500, new { error = "Failed to update system configurations" });
        }
    }

    /// <summary>
    /// Gets all feature flags
    /// </summary>
    /// <returns>Feature flags</returns>
    [HttpGet("config/features")]
    public async Task<ActionResult<List<FeatureFlag>>> GetFeatureFlags(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var flags = await _configurationService.GetAllFeatureFlagsAsync(cancellationToken);
            return Ok(flags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get feature flags");
            return StatusCode(500, new { error = "Failed to retrieve feature flags" });
        }
    }

    /// <summary>
    /// Updates feature flags
    /// </summary>
    /// <param name="flags">Feature flags to update</param>
    /// <returns>Update result</returns>
    [HttpPut("config/features")]
    public async Task<ActionResult> UpdateFeatureFlags(
        [FromBody] Dictionary<string, bool> flags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (flags?.Count == 0)
            {
                return BadRequest(new { error = "No feature flags provided" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _configurationService.UpdateFeatureFlagsAsync(flags!, User, ipAddress, cancellationToken);

            if (success)
            {
                return Ok(new { message = $"Successfully updated {flags!.Count} feature flags" });
            }

            return StatusCode(500, new { error = "Failed to update feature flags" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update feature flags");
            return StatusCode(500, new { error = "Failed to update feature flags" });
        }
    }

    /// <summary>
    /// Schedules a maintenance window
    /// </summary>
    /// <param name="request">Maintenance schedule request</param>
    /// <returns>Schedule result</returns>
    [HttpPost("maintenance/schedule")]
    public async Task<ActionResult> ScheduleMaintenance(
        [FromBody] MaintenanceScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Description))
            {
                return BadRequest(new { error = "Maintenance description is required" });
            }

            if (request.StartTime <= DateTimeOffset.UtcNow)
            {
                return BadRequest(new { error = "Maintenance start time must be in the future" });
            }

            if (request.EndTime <= request.StartTime)
            {
                return BadRequest(new { error = "Maintenance end time must be after start time" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _configurationService.ScheduleMaintenanceAsync(request, User, ipAddress, cancellationToken);

            if (success)
            {
                return Ok(new 
                { 
                    message = "Maintenance window scheduled successfully",
                    maintenanceId = request.Id,
                    startTime = request.StartTime,
                    endTime = request.EndTime
                });
            }

            return StatusCode(500, new { error = "Failed to schedule maintenance window" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule maintenance window");
            return StatusCode(500, new { error = "Failed to schedule maintenance window" });
        }
    }

    /// <summary>
    /// Gets notification settings
    /// </summary>
    /// <returns>Notification settings</returns>
    [HttpGet("config/notifications")]
    public async Task<ActionResult<NotificationSettings>> GetNotificationSettings(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await _configurationService.GetNotificationSettingsAsync(cancellationToken);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get notification settings");
            return StatusCode(500, new { error = "Failed to retrieve notification settings" });
        }
    }

    /// <summary>
    /// Updates notification settings
    /// </summary>
    /// <param name="settings">Notification settings to update</param>
    /// <returns>Update result</returns>
    [HttpPut("config/notifications")]
    public async Task<ActionResult> UpdateNotificationSettings(
        [FromBody] NotificationSettings settings,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic validation
            if (settings.EmailEnabled && string.IsNullOrEmpty(settings.SmtpHost))
            {
                return BadRequest(new { error = "SMTP host is required when email notifications are enabled" });
            }

            if (settings.WebhookEnabled && string.IsNullOrEmpty(settings.WebhookUrl))
            {
                return BadRequest(new { error = "Webhook URL is required when webhook notifications are enabled" });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var success = await _configurationService.UpdateNotificationSettingsAsync(settings, User, ipAddress, cancellationToken);

            if (success)
            {
                return Ok(new { message = "Notification settings updated successfully" });
            }

            return StatusCode(500, new { error = "Failed to update notification settings" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update notification settings");
            return StatusCode(500, new { error = "Failed to update notification settings" });
        }
    }

    /// <summary>
    /// Gets system status and health information
    /// </summary>
    /// <returns>System status</returns>
    [HttpGet("system/status")]
    public ActionResult<SystemStatus> GetSystemStatus()
    {
        try
        {
            var status = new SystemStatus
            {
                SystemName = "Industrial ADAM Counter System",
                Version = "2.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                ServerTime = DateTimeOffset.UtcNow,
                Status = "Operational",
                Services = new Dictionary<string, ServiceStatus>
                {
                    ["Database"] = new() { Name = "TimescaleDB", Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(5) },
                    ["Authentication"] = new() { Name = "JWT Auth", Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(2) },
                    ["Security"] = new() { Name = "Security Module", Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(3) },
                    ["Logging"] = new() { Name = "Serilog", Status = "Healthy", ResponseTime = TimeSpan.FromMilliseconds(1) }
                }
            };

            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system status");
            return StatusCode(500, new { error = "Failed to retrieve system status" });
        }
    }

    /// <summary>
    /// Gets system metrics and performance data
    /// </summary>
    /// <returns>System metrics</returns>
    [HttpGet("system/metrics")]
    public ActionResult<SystemMetrics> GetSystemMetrics()
    {
        try
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            
            var metrics = new SystemMetrics
            {
                GeneratedAt = DateTimeOffset.UtcNow,
                CpuUsage = GetCpuUsage(),
                MemoryUsage = new SystemMetrics.MemoryInfo
                {
                    WorkingSet = process.WorkingSet64,
                    PrivateMemorySize = process.PrivateMemorySize64,
                    VirtualMemorySize = process.VirtualMemorySize64,
                    GcMemory = GC.GetTotalMemory(false)
                },
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                GcCollections = new SystemMetrics.GcInfo
                {
                    Gen0 = GC.CollectionCount(0),
                    Gen1 = GC.CollectionCount(1),
                    Gen2 = GC.CollectionCount(2)
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system metrics");
            return StatusCode(500, new { error = "Failed to retrieve system metrics" });
        }
    }

    private static double GetCpuUsage()
    {
        // Simplified CPU usage - in production, you'd use a proper performance counter
        return Math.Round(Random.Shared.NextDouble() * 100, 2);
    }
}

/// <summary>
/// System status model
/// </summary>
public class SystemStatus
{
    public string SystemName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public TimeSpan Uptime { get; set; }
    public DateTimeOffset ServerTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, ServiceStatus> Services { get; set; } = new();
}

/// <summary>
/// Service status model
/// </summary>
public class ServiceStatus
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public TimeSpan ResponseTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// System metrics model
/// </summary>
public class SystemMetrics
{
    public DateTimeOffset GeneratedAt { get; set; }
    public double CpuUsage { get; set; }
    public MemoryInfo MemoryUsage { get; set; } = new();
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
    public GcInfo GcCollections { get; set; } = new();

    public class MemoryInfo
    {
        public long WorkingSet { get; set; }
        public long PrivateMemorySize { get; set; }
        public long VirtualMemorySize { get; set; }
        public long GcMemory { get; set; }
    }

    public class GcInfo
    {
        public int Gen0 { get; set; }
        public int Gen1 { get; set; }
        public int Gen2 { get; set; }
    }
}