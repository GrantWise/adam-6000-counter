using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Checks;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Health;

/// <summary>
/// Comprehensive health check service for monitoring system components
/// </summary>
public sealed class HealthCheckService : IHealthCheckService, IDisposable
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;
    private readonly ApplicationHealthCheck _applicationHealthCheck;
    private readonly InfluxDbHealthCheck _influxDbHealthCheck;
    private readonly SystemResourceHealthCheck _systemResourceHealthCheck;
    private readonly ConcurrentDictionary<string, Func<CancellationToken, Task<ComponentHealth>>> _customHealthChecks;
    private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _componentDependencies;
    private readonly ConcurrentDictionary<string, ComponentHealth> _lastHealthResults;
    private readonly DateTime _startTime;
    private HealthStatus _lastSystemHealthStatus = HealthStatus.Healthy;
    private int _lastSystemHealthScore = 100;

    /// <summary>
    /// Event triggered when component health status changes
    /// </summary>
    public event EventHandler<ComponentHealthChangedEventArgs>? ComponentHealthChanged;

    /// <summary>
    /// Event triggered when overall system health status changes
    /// </summary>
    public event EventHandler<SystemHealthChangedEventArgs>? SystemHealthChanged;

    /// <summary>
    /// Initialize health check service
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    /// <param name="applicationHealthCheck">Application health check</param>
    /// <param name="influxDbHealthCheck">InfluxDB health check</param>
    /// <param name="systemResourceHealthCheck">System resource health check</param>
    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService,
        ApplicationHealthCheck applicationHealthCheck,
        InfluxDbHealthCheck influxDbHealthCheck,
        SystemResourceHealthCheck systemResourceHealthCheck)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        _applicationHealthCheck = applicationHealthCheck ?? throw new ArgumentNullException(nameof(applicationHealthCheck));
        _influxDbHealthCheck = influxDbHealthCheck ?? throw new ArgumentNullException(nameof(influxDbHealthCheck));
        _systemResourceHealthCheck = systemResourceHealthCheck ?? throw new ArgumentNullException(nameof(systemResourceHealthCheck));

        _customHealthChecks = new ConcurrentDictionary<string, Func<CancellationToken, Task<ComponentHealth>>>();
        _componentDependencies = new ConcurrentDictionary<string, IReadOnlyList<string>>();
        _lastHealthResults = new ConcurrentDictionary<string, ComponentHealth>();
        _startTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Perform a comprehensive health check of all system components
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete health response with all component statuses</returns>
    public async Task<OperationResult<HealthResponse>> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting comprehensive health check");

            var componentResults = new Dictionary<string, ComponentHealth>();
            var systemWarnings = new List<string>();
            var systemRecommendations = new List<string>();
            var systemMetrics = new Dictionary<string, object>();

            // Execute all health checks in parallel for better performance
            var healthCheckTasks = new List<Task<(string Name, ComponentHealth Health)>>
            {
                ExecuteComponentHealthCheck("Application", _applicationHealthCheck.CheckHealthAsync, cancellationToken),
                ExecuteComponentHealthCheck("InfluxDB", _influxDbHealthCheck.CheckHealthAsync, cancellationToken),
                ExecuteComponentHealthCheck("SystemResources", _systemResourceHealthCheck.CheckHealthAsync, cancellationToken)
            };

            // Add custom health checks
            foreach (var customCheck in _customHealthChecks)
            {
                healthCheckTasks.Add(ExecuteComponentHealthCheck(customCheck.Key, customCheck.Value, cancellationToken));
            }

            // Wait for all health checks to complete
            var healthCheckResults = await Task.WhenAll(healthCheckTasks);

            // Process results and detect changes
            foreach (var (name, health) in healthCheckResults)
            {
                componentResults[name] = health;

                // Detect and report component health changes
                if (_lastHealthResults.TryGetValue(name, out var lastHealth) && lastHealth.Status != health.Status)
                {
                    ComponentHealthChanged?.Invoke(this, new ComponentHealthChangedEventArgs
                    {
                        ComponentName = name,
                        PreviousStatus = lastHealth.Status,
                        CurrentStatus = health.Status,
                        Timestamp = DateTimeOffset.UtcNow,
                        ComponentHealth = health
                    });
                }

                _lastHealthResults[name] = health;

                // Aggregate warnings and recommendations
                systemWarnings.AddRange(health.Warnings);
                systemRecommendations.AddRange(health.Recommendations);
            }

            // Calculate overall system health
            var (overallStatus, overallScore) = CalculateOverallHealth(componentResults);

            // Add system-wide metrics
            PopulateSystemMetrics(systemMetrics, componentResults);

            // Create version info
            var versionInfo = CreateVersionInfo();

            // Create environment info
            var environmentInfo = CreateEnvironmentInfo();

            // Create health response
            var healthResponse = new HealthResponse
            {
                Status = overallStatus,
                HealthScore = overallScore,
                Timestamp = DateTimeOffset.UtcNow,
                Duration = stopwatch.Elapsed,
                Uptime = DateTime.UtcNow - _startTime,
                Components = componentResults,
                Metrics = systemMetrics,
                Warnings = systemWarnings.Distinct().ToList(),
                Recommendations = systemRecommendations.Distinct().ToList(),
                Version = versionInfo,
                Environment = environmentInfo
            };

            // Detect and report system health changes
            if (_lastSystemHealthStatus != overallStatus || Math.Abs(_lastSystemHealthScore - overallScore) >= 10)
            {
                SystemHealthChanged?.Invoke(this, new SystemHealthChangedEventArgs
                {
                    PreviousStatus = _lastSystemHealthStatus,
                    CurrentStatus = overallStatus,
                    PreviousHealthScore = _lastSystemHealthScore,
                    CurrentHealthScore = overallScore,
                    Timestamp = DateTimeOffset.UtcNow,
                    HealthResponse = healthResponse
                });

                _lastSystemHealthStatus = overallStatus;
                _lastSystemHealthScore = overallScore;
            }

            _logger.LogInformation(
                "Health check completed in {Duration}ms. Status: {Status}, Score: {Score}, Components: {ComponentCount}",
                stopwatch.ElapsedMilliseconds, overallStatus, overallScore, componentResults.Count);

            return OperationResult<HealthResponse>.Success(healthResponse, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-100",
                "Comprehensive health check failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<HealthResponse>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Check health of a specific component
    /// </summary>
    /// <param name="componentName">Name of the component to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health status of the specified component</returns>
    public async Task<OperationResult<ComponentHealth>> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Checking health for component: {ComponentName}", componentName);

            ComponentHealth componentHealth = componentName switch
            {
                "Application" => await _applicationHealthCheck.CheckHealthAsync(cancellationToken),
                "InfluxDB" => await _influxDbHealthCheck.CheckHealthAsync(cancellationToken),
                "SystemResources" => await _systemResourceHealthCheck.CheckHealthAsync(cancellationToken),
                _ when _customHealthChecks.TryGetValue(componentName, out var customCheck) => await customCheck(cancellationToken),
                _ => ComponentHealth.Unhealthy(
                    componentName,
                    stopwatch.Elapsed,
                    $"Unknown component: {componentName}",
                    0,
                    new[] { "Verify component name is correct", "Check available components list" })
            };

            _logger.LogDebug(
                "Component health check completed for {ComponentName} in {Duration}ms. Status: {Status}",
                componentName, stopwatch.ElapsedMilliseconds, componentHealth.Status);

            return OperationResult<ComponentHealth>.Success(componentHealth, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-101",
                $"Component health check failed for {componentName}",
                new Dictionary<string, object>
                {
                    ["ComponentName"] = componentName,
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<ComponentHealth>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Get a quick health status without detailed metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quick health status for load balancer health checks</returns>
    public async Task<OperationResult<HealthStatus>> GetQuickHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Quick check of critical components only
            var applicationHealth = await _applicationHealthCheck.CheckHealthAsync(cancellationToken);

            // Return the most critical status
            var quickStatus = applicationHealth.Status;

            _logger.LogDebug(
                "Quick health status check completed in {Duration}ms. Status: {Status}",
                stopwatch.ElapsedMilliseconds, quickStatus);

            return OperationResult<HealthStatus>.Success(quickStatus, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-102",
                "Quick health status check failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<HealthStatus>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Get detailed health metrics for monitoring systems
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed health metrics suitable for Prometheus or similar monitoring</returns>
    public async Task<OperationResult<IReadOnlyDictionary<string, object>>> GetHealthMetricsAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var healthResult = await CheckHealthAsync(cancellationToken);
            if (!healthResult.IsSuccess)
            {
                return OperationResult<IReadOnlyDictionary<string, object>>.Failure(
                    new InvalidOperationException(healthResult.ErrorMessage),
                    stopwatch.Elapsed,
                    new Dictionary<string, object>());
            }

            var metrics = new Dictionary<string, object>();
            var healthResponse = healthResult.Value;

            // Overall health metrics
            metrics["system_health_status"] = (int)healthResponse.Status;
            metrics["system_health_score"] = healthResponse.HealthScore;
            metrics["system_uptime_seconds"] = healthResponse.Uptime.TotalSeconds;
            metrics["health_check_duration_milliseconds"] = healthResponse.Duration.TotalMilliseconds;

            // Component health metrics
            foreach (var component in healthResponse.Components)
            {
                var componentPrefix = $"component_{component.Key.ToLowerInvariant()}";
                metrics[$"{componentPrefix}_health_status"] = (int)component.Value.Status;
                metrics[$"{componentPrefix}_health_score"] = component.Value.HealthScore;
                metrics[$"{componentPrefix}_check_duration_milliseconds"] = component.Value.CheckDuration.TotalMilliseconds;

                // Add component-specific metrics
                foreach (var metric in component.Value.Metrics)
                {
                    if (metric.Value is IConvertible convertible)
                    {
                        metrics[$"{componentPrefix}_{metric.Key.ToLowerInvariant()}"] = convertible;
                    }
                }
            }

            // System-wide metrics
            foreach (var metric in healthResponse.Metrics)
            {
                if (metric.Value is IConvertible convertible)
                {
                    metrics[$"system_{metric.Key.ToLowerInvariant()}"] = convertible;
                }
            }

            _logger.LogDebug(
                "Health metrics retrieved in {Duration}ms. Metric count: {MetricCount}",
                stopwatch.ElapsedMilliseconds, metrics.Count);

            return OperationResult<IReadOnlyDictionary<string, object>>.Success(
                metrics,
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-103",
                "Health metrics retrieval failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds
                });

            return OperationResult<IReadOnlyDictionary<string, object>>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Get list of available components that can be health checked
    /// </summary>
    /// <returns>List of component names that support health checking</returns>
    public OperationResult<IReadOnlyList<string>> GetAvailableComponents()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var components = new List<string>
            {
                "Application",
                "InfluxDB",
                "SystemResources"
            };

            // Add custom components
            components.AddRange(_customHealthChecks.Keys);

            _logger.LogDebug("Retrieved {ComponentCount} available components", components.Count);

            return OperationResult<IReadOnlyList<string>>.Success(
                components.AsReadOnly(),
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-104",
                "Failed to retrieve available components",
                new Dictionary<string, object>());

            return OperationResult<IReadOnlyList<string>>.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Register a custom health check for a component
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <param name="healthCheck">Health check function to execute</param>
    /// <param name="dependencies">Component dependencies</param>
    /// <returns>Success if registration was successful</returns>
    public OperationResult RegisterComponentHealthCheck(
        string componentName,
        Func<CancellationToken, Task<ComponentHealth>> healthCheck,
        IReadOnlyList<string>? dependencies = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrWhiteSpace(componentName))
            {
                return OperationResult.Failure(
                    new ArgumentException("Component name cannot be null or empty", nameof(componentName)),
                    stopwatch.Elapsed,
                    new Dictionary<string, object>());
            }

            if (healthCheck == null)
            {
                return OperationResult.Failure(
                    new ArgumentNullException(nameof(healthCheck)),
                    stopwatch.Elapsed,
                    new Dictionary<string, object>());
            }

            _customHealthChecks[componentName] = healthCheck;

            if (dependencies != null)
            {
                _componentDependencies[componentName] = dependencies;
            }

            _logger.LogInformation("Registered custom health check for component: {ComponentName}", componentName);

            return OperationResult.Success(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-105",
                $"Failed to register health check for component {componentName}",
                new Dictionary<string, object>
                {
                    ["ComponentName"] = componentName
                });

            return OperationResult.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Unregister a custom health check for a component
    /// </summary>
    /// <param name="componentName">Name of the component</param>
    /// <returns>Success if unregistration was successful</returns>
    public OperationResult UnregisterComponentHealthCheck(string componentName)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var removed = _customHealthChecks.TryRemove(componentName, out _);
            _componentDependencies.TryRemove(componentName, out _);
            _lastHealthResults.TryRemove(componentName, out _);

            if (removed)
            {
                _logger.LogInformation("Unregistered custom health check for component: {ComponentName}", componentName);
            }
            else
            {
                _logger.LogWarning("Attempted to unregister non-existent health check for component: {ComponentName}", componentName);
            }

            return OperationResult.Success(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-106",
                $"Failed to unregister health check for component {componentName}",
                new Dictionary<string, object>
                {
                    ["ComponentName"] = componentName
                });

            return OperationResult.Failure(
                errorMessage.OriginalException ?? ex,
                stopwatch.Elapsed,
                new Dictionary<string, object>());
        }
    }

    /// <summary>
    /// Execute a component health check with error handling
    /// </summary>
    /// <param name="componentName">Component name</param>
    /// <param name="healthCheck">Health check function</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Component name and health result</returns>
    private async Task<(string Name, ComponentHealth Health)> ExecuteComponentHealthCheck(
        string componentName,
        Func<CancellationToken, Task<ComponentHealth>> healthCheck,
        CancellationToken cancellationToken)
    {
        try
        {
            var health = await healthCheck(cancellationToken);
            return (componentName, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for component: {ComponentName}", componentName);

            var failedHealth = ComponentHealth.Critical(
                componentName,
                TimeSpan.Zero,
                $"Health check failed: {ex.Message}",
                new[] { "Investigate health check implementation", "Check component availability" });

            return (componentName, failedHealth);
        }
    }

    /// <summary>
    /// Calculate overall system health based on component health
    /// </summary>
    /// <param name="componentResults">Component health results</param>
    /// <returns>Overall status and score</returns>
    private static (HealthStatus Status, int Score) CalculateOverallHealth(
        Dictionary<string, ComponentHealth> componentResults)
    {
        if (componentResults.Count == 0)
        {
            return (HealthStatus.Critical, 0);
        }

        // Find the worst status
        var worstStatus = componentResults.Values.Max(c => c.Status);

        // Calculate weighted average score
        var totalScore = componentResults.Values.Sum(c => c.HealthScore);
        var averageScore = totalScore / componentResults.Count;

        // Adjust overall status based on critical components
        if (componentResults.TryGetValue("Application", out var appHealth) && appHealth.Status >= HealthStatus.Unhealthy)
        {
            worstStatus = HealthStatus.Critical;
        }

        return (worstStatus, averageScore);
    }

    /// <summary>
    /// Populate system-wide metrics
    /// </summary>
    /// <param name="systemMetrics">System metrics dictionary</param>
    /// <param name="componentResults">Component results</param>
    private void PopulateSystemMetrics(
        Dictionary<string, object> systemMetrics,
        Dictionary<string, ComponentHealth> componentResults)
    {
        systemMetrics["ComponentCount"] = componentResults.Count;
        systemMetrics["HealthyComponentCount"] = componentResults.Values.Count(c => c.Status == HealthStatus.Healthy);
        systemMetrics["DegradedComponentCount"] = componentResults.Values.Count(c => c.Status == HealthStatus.Degraded);
        systemMetrics["UnhealthyComponentCount"] = componentResults.Values.Count(c => c.Status == HealthStatus.Unhealthy);
        systemMetrics["CriticalComponentCount"] = componentResults.Values.Count(c => c.Status == HealthStatus.Critical);
        systemMetrics["TotalWarnings"] = componentResults.Values.Sum(c => c.Warnings.Count);
        systemMetrics["TotalRecommendations"] = componentResults.Values.Sum(c => c.Recommendations.Count);
        systemMetrics["CustomHealthCheckCount"] = _customHealthChecks.Count;
        systemMetrics["ConfiguredDevices"] = _config.Value.Devices.Count;
        systemMetrics["DemoMode"] = _config.Value.DemoMode;
    }

    /// <summary>
    /// Create version information
    /// </summary>
    /// <returns>Version information</returns>
    private static VersionInfo CreateVersionInfo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "Unknown";
        var buildTime = File.GetCreationTimeUtc(assembly.Location);

        return new VersionInfo
        {
            ApplicationVersion = version,
            RuntimeVersion = Environment.Version.ToString(),
            BuildTimestamp = new DateTimeOffset(buildTime, TimeSpan.Zero),
            GitCommitHash = null, // Could be populated from build process
            GitBranch = null // Could be populated from build process
        };
    }

    /// <summary>
    /// Create environment information
    /// </summary>
    /// <returns>Environment information</returns>
    private static EnvironmentInfo CreateEnvironmentInfo()
    {
        var process = Process.GetCurrentProcess();

        return new EnvironmentInfo
        {
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.ToString(),
            ProcessId = process.Id,
            ProcessorCount = Environment.ProcessorCount,
            TotalMemoryBytes = GC.GetTotalMemory(false),
            AvailableMemoryBytes = 0 // Would need platform-specific implementation
        };
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        _systemResourceHealthCheck?.Dispose();
    }
}
