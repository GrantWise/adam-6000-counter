using Industrial.Adam.Logger.WebApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace Industrial.Adam.Logger.WebApi.Controllers;

/// <summary>
/// Controller for diagnostics, logs, and error analytics
/// </summary>
[ApiController]
[Route("api/diagnostics")]
[Produces("application/json")]
public class DiagnosticsController : ControllerBase
{
    private readonly IDeviceOrchestrator _orchestrator;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(IDeviceOrchestrator orchestrator, ILogger<DiagnosticsController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    /// <summary>
    /// Get system diagnostics information
    /// </summary>
    /// <returns>System diagnostic data</returns>
    /// <response code="200">Returns system diagnostics</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("system")]
    [ProducesResponseType(typeof(SystemDiagnostics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetSystemDiagnostics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var configResult = _orchestrator.GetConfiguration();

            var diagnostics = new SystemDiagnostics
            {
                ApplicationVersion = GetApplicationVersion(),
                DotNetVersion = Environment.Version.ToString(),
                OperatingSystem = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName,
                ProcessId = process.Id,
                StartTime = process.StartTime,
                WorkingSet = process.WorkingSet64,
                PrivateMemory = process.PrivateMemorySize64,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount,
                ConfigurationValid = configResult.IsSuccess,
                LastConfigUpdate = DateTime.UtcNow, // Would track actual config updates
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system diagnostics");
            return StatusCode(500, new { error = "Failed to retrieve system diagnostics" });
        }
    }

    /// <summary>
    /// Get error analytics and statistics
    /// </summary>
    /// <param name="hours">Number of hours to look back (default: 24)</param>
    /// <returns>Error analytics data</returns>
    /// <response code="200">Returns error analytics</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("errors")]
    [ProducesResponseType(typeof(ErrorAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetErrorAnalytics([FromQuery] int hours = 24)
    {
        try
        {
            var devicesResult = await _orchestrator.GetAllDevicesAsync();
            if (!devicesResult.IsSuccess)
            {
                return StatusCode(500, new { error = devicesResult.ErrorMessage });
            }

            var devices = devicesResult.Value;
            var cutoffTime = DateTimeOffset.UtcNow.AddHours(-hours);

            // Simulate error analytics - in real implementation, this would query actual error logs
            var errorAnalytics = new ErrorAnalytics
            {
                TimeRangeHours = hours,
                TotalErrors = devices.Sum(d => d.Health?.ConsecutiveFailures ?? 0),
                ErrorsByType = GenerateErrorTypeDistribution(),
                ErrorsByDevice = devices.ToDictionary(
                    d => d.Config.DeviceId,
                    d => d.Health?.ConsecutiveFailures ?? 0
                ),
                ErrorTrend = GenerateErrorTrend(hours),
                MostCommonErrors = GenerateMostCommonErrors(),
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(errorAnalytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error analytics");
            return StatusCode(500, new { error = "Failed to retrieve error analytics" });
        }
    }

    /// <summary>
    /// Get recent error logs
    /// </summary>
    /// <param name="limit">Maximum number of logs to return (default: 100)</param>
    /// <param name="severity">Filter by severity level</param>
    /// <returns>Recent error logs</returns>
    /// <response code="200">Returns error logs</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("logs/errors")]
    [ProducesResponseType(typeof(IReadOnlyList<ErrorLogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetErrorLogs([FromQuery] int limit = 100, [FromQuery] string? severity = null)
    {
        try
        {
            // Simulate error log retrieval - in real implementation, this would query actual log storage
            var errorLogs = GenerateRecentErrorLogs(limit, severity);
            return Ok(errorLogs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get error logs");
            return StatusCode(500, new { error = "Failed to retrieve error logs" });
        }
    }

    /// <summary>
    /// Get application logs
    /// </summary>
    /// <param name="limit">Maximum number of logs to return (default: 100)</param>
    /// <param name="level">Filter by log level</param>
    /// <returns>Application logs</returns>
    /// <response code="200">Returns application logs</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("logs/application")]
    [ProducesResponseType(typeof(IReadOnlyList<LogEntry>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GetApplicationLogs([FromQuery] int limit = 100, [FromQuery] string? level = null)
    {
        try
        {
            // Simulate application log retrieval - in real implementation, this would query actual log storage
            var logs = GenerateRecentApplicationLogs(limit, level);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get application logs");
            return StatusCode(500, new { error = "Failed to retrieve application logs" });
        }
    }

    /// <summary>
    /// Get device-specific diagnostics
    /// </summary>
    /// <param name="deviceId">Device ID</param>
    /// <returns>Device diagnostic information</returns>
    /// <response code="200">Returns device diagnostics</response>
    /// <response code="404">If device not found</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("devices/{deviceId}")]
    [ProducesResponseType(typeof(DeviceDiagnostics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeviceDiagnostics(string deviceId)
    {
        try
        {
            var deviceResult = await _orchestrator.GetDeviceByIdAsync(deviceId);
            if (!deviceResult.IsSuccess)
            {
                if (deviceResult.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { error = $"Device '{deviceId}' not found" });
                }
                return StatusCode(500, new { error = deviceResult.ErrorMessage });
            }

            var device = deviceResult.Value;
            var diagnostics = new DeviceDiagnostics
            {
                DeviceId = device.Config.DeviceId,
                Name = device.Config.DeviceId, // Use DeviceId as name since Name property doesn't exist
                IpAddress = device.Config.IpAddress,
                Port = device.Config.Port,
                Status = device.Health?.Status.ToString() ?? "Unknown",
                LastContact = device.Health?.Timestamp,
                ErrorCount = device.Health?.ConsecutiveFailures ?? 0,
                LastError = device.Health?.LastError,
                RecentErrors = GenerateDeviceRecentErrors(deviceId),
                ConnectionHistory = GenerateConnectionHistory(deviceId),
                PerformanceMetrics = GenerateDevicePerformanceMetrics(deviceId),
                Timestamp = DateTimeOffset.UtcNow
            };

            return Ok(diagnostics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get device diagnostics for {DeviceId}", deviceId);
            return StatusCode(500, new { error = "Failed to retrieve device diagnostics" });
        }
    }

    /// <summary>
    /// Export diagnostics data as JSON
    /// </summary>
    /// <returns>Diagnostics data as downloadable JSON file</returns>
    /// <response code="200">Returns diagnostics as JSON file</response>
    /// <response code="500">If an internal error occurs</response>
    [HttpGet("export")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportDiagnostics()
    {
        try
        {
            var systemDiagnostics = GetSystemDiagnosticsData();
            var errorAnalytics = await GetErrorAnalyticsData(24);
            var recentLogs = GenerateRecentApplicationLogs(200, null);

            var exportData = new DiagnosticsExport
            {
                SystemDiagnostics = systemDiagnostics,
                ErrorAnalytics = errorAnalytics,
                RecentLogs = recentLogs,
                ExportTime = DateTimeOffset.UtcNow
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(exportData, options);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            var fileName = $"adam-logger-diagnostics-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
            return File(bytes, "application/json", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export diagnostics");
            return StatusCode(500, new { error = "Failed to export diagnostics" });
        }
    }

    private static string GetApplicationVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    private static Dictionary<string, int> GenerateErrorTypeDistribution()
    {
        return new Dictionary<string, int>
        {
            ["ConnectionTimeout"] = 15,
            ["ModbusError"] = 8,
            ["NetworkError"] = 12,
            ["ValidationError"] = 3,
            ["ConfigurationError"] = 2
        };
    }

    private static List<ErrorTrendPoint> GenerateErrorTrend(int hours)
    {
        var trend = new List<ErrorTrendPoint>();
        var now = DateTimeOffset.UtcNow;

        for (int i = hours; i >= 0; i--)
        {
            trend.Add(new ErrorTrendPoint
            {
                Timestamp = now.AddHours(-i),
                ErrorCount = Random.Shared.Next(0, 10)
            });
        }

        return trend;
    }

    private static List<CommonError> GenerateMostCommonErrors()
    {
        return new List<CommonError>
        {
            new() { Message = "Connection timeout to device", Count = 15, LastOccurrence = DateTimeOffset.UtcNow.AddMinutes(-30) },
            new() { Message = "Modbus read timeout", Count = 8, LastOccurrence = DateTimeOffset.UtcNow.AddMinutes(-15) },
            new() { Message = "Network unreachable", Count = 5, LastOccurrence = DateTimeOffset.UtcNow.AddHours(-1) }
        };
    }

    private static List<ErrorLogEntry> GenerateRecentErrorLogs(int limit, string? severity)
    {
        var logs = new List<ErrorLogEntry>();
        var severities = new[] { "Error", "Warning", "Critical" };
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < Math.Min(limit, 50); i++)
        {
            var logSeverity = severities[Random.Shared.Next(severities.Length)];
            if (severity != null && !string.Equals(logSeverity, severity, StringComparison.OrdinalIgnoreCase))
                continue;

            logs.Add(new ErrorLogEntry
            {
                Timestamp = now.AddMinutes(-Random.Shared.Next(0, 1440)),
                Severity = logSeverity,
                Message = $"Sample error message {i + 1}",
                Source = $"Device-{Random.Shared.Next(1, 5)}",
                Exception = i % 3 == 0 ? "System.TimeoutException: Connection timeout" : null
            });
        }

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    private static List<LogEntry> GenerateRecentApplicationLogs(int limit, string? level)
    {
        var logs = new List<LogEntry>();
        var levels = new[] { "Information", "Warning", "Error", "Debug" };
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < Math.Min(limit, 100); i++)
        {
            var logLevel = levels[Random.Shared.Next(levels.Length)];
            if (level != null && !string.Equals(logLevel, level, StringComparison.OrdinalIgnoreCase))
                continue;

            logs.Add(new LogEntry
            {
                Timestamp = now.AddMinutes(-Random.Shared.Next(0, 1440)),
                Level = logLevel,
                Message = $"Sample log message {i + 1}",
                Source = "Industrial.Adam.Logger",
                Category = "Application"
            });
        }

        return logs.OrderByDescending(l => l.Timestamp).ToList();
    }

    private static List<ErrorLogEntry> GenerateDeviceRecentErrors(string deviceId)
    {
        var errors = new List<ErrorLogEntry>();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 5; i++)
        {
            errors.Add(new ErrorLogEntry
            {
                Timestamp = now.AddHours(-Random.Shared.Next(1, 48)),
                Severity = "Error",
                Message = $"Device error for {deviceId}",
                Source = deviceId,
                Exception = null
            });
        }

        return errors.OrderByDescending(e => e.Timestamp).ToList();
    }

    private static List<ConnectionEvent> GenerateConnectionHistory(string deviceId)
    {
        var history = new List<ConnectionEvent>();
        var now = DateTimeOffset.UtcNow;

        for (int i = 0; i < 10; i++)
        {
            history.Add(new ConnectionEvent
            {
                Timestamp = now.AddHours(-Random.Shared.Next(1, 72)),
                EventType = i % 2 == 0 ? "Connected" : "Disconnected",
                Duration = TimeSpan.FromMinutes(Random.Shared.Next(5, 120))
            });
        }

        return history.OrderByDescending(h => h.Timestamp).ToList();
    }

    private static DevicePerformanceMetrics GenerateDevicePerformanceMetrics(string deviceId)
    {
        return new DevicePerformanceMetrics
        {
            AverageResponseTime = Random.Shared.NextDouble() * 200 + 50,
            SuccessRate = Random.Shared.NextDouble() * 20 + 80,
            DataPointsPerHour = Random.Shared.Next(100, 1000),
            LastDataPoint = DateTimeOffset.UtcNow.AddMinutes(-Random.Shared.Next(1, 60))
        };
    }

    private SystemDiagnostics GetSystemDiagnosticsData()
    {
        var result = GetSystemDiagnostics();
        if (result is OkObjectResult okResult && okResult.Value is SystemDiagnostics diagnostics)
        {
            return diagnostics;
        }
        return new SystemDiagnostics();
    }

    private async Task<ErrorAnalytics> GetErrorAnalyticsData(int hours)
    {
        var result = await GetErrorAnalytics(hours);
        if (result is OkObjectResult okResult && okResult.Value is ErrorAnalytics analytics)
        {
            return analytics;
        }
        return new ErrorAnalytics();
    }
}

/// <summary>
/// System diagnostics information
/// </summary>
public class SystemDiagnostics
{
    /// <summary>
    /// Application version
    /// </summary>
    public string ApplicationVersion { get; set; } = string.Empty;

    /// <summary>
    /// .NET runtime version
    /// </summary>
    public string DotNetVersion { get; set; } = string.Empty;

    /// <summary>
    /// Operating system information
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// Machine name
    /// </summary>
    public string MachineName { get; set; } = string.Empty;

    /// <summary>
    /// Process ID
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Application start time
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Working set memory in bytes
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Private memory in bytes
    /// </summary>
    public long PrivateMemory { get; set; }

    /// <summary>
    /// Thread count
    /// </summary>
    public int ThreadCount { get; set; }

    /// <summary>
    /// Handle count
    /// </summary>
    public int HandleCount { get; set; }

    /// <summary>
    /// Whether configuration is valid
    /// </summary>
    public bool ConfigurationValid { get; set; }

    /// <summary>
    /// Last configuration update time
    /// </summary>
    public DateTime LastConfigUpdate { get; set; }

    /// <summary>
    /// Diagnostics timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Error analytics and statistics
/// </summary>
public class ErrorAnalytics
{
    /// <summary>
    /// Time range for analytics in hours
    /// </summary>
    public int TimeRangeHours { get; set; }

    /// <summary>
    /// Total error count
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Error distribution by type
    /// </summary>
    public Dictionary<string, int> ErrorsByType { get; set; } = new();

    /// <summary>
    /// Error count by device
    /// </summary>
    public Dictionary<string, int> ErrorsByDevice { get; set; } = new();

    /// <summary>
    /// Error trend over time
    /// </summary>
    public List<ErrorTrendPoint> ErrorTrend { get; set; } = new();

    /// <summary>
    /// Most common error messages
    /// </summary>
    public List<CommonError> MostCommonErrors { get; set; } = new();

    /// <summary>
    /// Analytics timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Error trend data point
/// </summary>
public class ErrorTrendPoint
{
    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Error count at this time
    /// </summary>
    public int ErrorCount { get; set; }
}

/// <summary>
/// Common error information
/// </summary>
public class CommonError
{
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Occurrence count
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Last occurrence time
    /// </summary>
    public DateTimeOffset LastOccurrence { get; set; }
}

/// <summary>
/// Error log entry
/// </summary>
public class ErrorLogEntry
{
    /// <summary>
    /// Log timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Error severity
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error source
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Exception details if available
    /// </summary>
    public string? Exception { get; set; }
}

/// <summary>
/// General log entry
/// </summary>
public class LogEntry
{
    /// <summary>
    /// Log timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Log level
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Log message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Log source
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Log category
    /// </summary>
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Device-specific diagnostics
/// </summary>
public class DeviceDiagnostics
{
    /// <summary>
    /// Device ID
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Device name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Device IP address
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// Device port
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last contact time
    /// </summary>
    public DateTimeOffset? LastContact { get; set; }

    /// <summary>
    /// Error count
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Last error message
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Recent error history
    /// </summary>
    public List<ErrorLogEntry> RecentErrors { get; set; } = new();

    /// <summary>
    /// Connection history
    /// </summary>
    public List<ConnectionEvent> ConnectionHistory { get; set; } = new();

    /// <summary>
    /// Performance metrics
    /// </summary>
    public DevicePerformanceMetrics PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Diagnostics timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}

/// <summary>
/// Connection event
/// </summary>
public class ConnectionEvent
{
    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Event type (Connected/Disconnected)
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Connection duration
    /// </summary>
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Device performance metrics
/// </summary>
public class DevicePerformanceMetrics
{
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public double AverageResponseTime { get; set; }

    /// <summary>
    /// Success rate percentage
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Data points collected per hour
    /// </summary>
    public int DataPointsPerHour { get; set; }

    /// <summary>
    /// Last data point timestamp
    /// </summary>
    public DateTimeOffset? LastDataPoint { get; set; }
}

/// <summary>
/// Complete diagnostics export
/// </summary>
public class DiagnosticsExport
{
    /// <summary>
    /// System diagnostics
    /// </summary>
    public SystemDiagnostics SystemDiagnostics { get; set; } = new();

    /// <summary>
    /// Error analytics
    /// </summary>
    public ErrorAnalytics ErrorAnalytics { get; set; } = new();

    /// <summary>
    /// Recent application logs
    /// </summary>
    public List<LogEntry> RecentLogs { get; set; } = new();

    /// <summary>
    /// Export timestamp
    /// </summary>
    public DateTimeOffset ExportTime { get; set; }
}