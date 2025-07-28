using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Health.Models;
using Industrial.Adam.Logger.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Industrial.Adam.Logger.Health.Checks;

/// <summary>
/// Health check for InfluxDB database connectivity and performance
/// </summary>
public sealed class InfluxDbHealthCheck
{
    private readonly ILogger<InfluxDbHealthCheck> _logger;
    private readonly IOptions<AdamLoggerConfig> _config;
    private readonly IIndustrialErrorService _errorService;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initialize InfluxDB health check
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="config">Configuration options</param>
    /// <param name="errorService">Error service</param>
    /// <param name="httpClient">HTTP client for InfluxDB requests</param>
    public InfluxDbHealthCheck(
        ILogger<InfluxDbHealthCheck> logger,
        IOptions<AdamLoggerConfig> config,
        IIndustrialErrorService errorService,
        HttpClient httpClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _errorService = errorService ?? throw new ArgumentNullException(nameof(errorService));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Check InfluxDB health and connectivity
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>InfluxDB health status</returns>
    public async Task<ComponentHealth> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new Dictionary<string, object>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        try
        {
            var influxConfig = _config.Value.InfluxDb;
            if (influxConfig == null)
            {
                return ComponentHealth.Critical(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    "InfluxDB configuration is missing",
                    new[] { "Add InfluxDB configuration section to appsettings.json" },
                    metrics);
            }

            // Add configuration metrics
            metrics["ConfiguredUrl"] = influxConfig.Url;
            metrics["ConfiguredBucket"] = influxConfig.Bucket;
            metrics["ConfiguredOrganization"] = influxConfig.Organization;
            metrics["ConfiguredMeasurement"] = influxConfig.Measurement;
            metrics["WriteBatchSize"] = influxConfig.WriteBatchSize;
            metrics["FlushIntervalMs"] = influxConfig.FlushIntervalMs;

            // Validate URL format
            if (!Uri.TryCreate(influxConfig.Url, UriKind.Absolute, out var influxUri))
            {
                return ComponentHealth.Critical(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    $"Invalid InfluxDB URL format: {influxConfig.Url}",
                    new[] { "Verify InfluxDB URL format in configuration", "Ensure URL includes protocol (http/https)" },
                    metrics);
            }

            metrics["ParsedHost"] = influxUri.Host;
            metrics["ParsedPort"] = influxUri.Port;
            metrics["ParsedScheme"] = influxUri.Scheme;

            // Test network connectivity to InfluxDB host
            var networkResult = await TestNetworkConnectivity(influxUri.Host, cancellationToken);
            metrics["NetworkConnectivity"] = networkResult.IsConnected;
            metrics["NetworkLatencyMs"] = networkResult.LatencyMs;

            if (!networkResult.IsConnected)
            {
                return ComponentHealth.Critical(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    $"Cannot reach InfluxDB host: {influxUri.Host}",
                    new[]
                    {
                        "Verify InfluxDB server is running",
                        "Check network connectivity",
                        "Verify firewall settings",
                        "Confirm InfluxDB URL is correct"
                    },
                    metrics);
            }

            if (networkResult.LatencyMs > 1000)
            {
                warnings.Add($"High network latency to InfluxDB: {networkResult.LatencyMs}ms");
                recommendations.Add("Investigate network performance to InfluxDB server");
            }

            // Test HTTP connectivity to InfluxDB
            var httpResult = await TestHttpConnectivity(influxUri, cancellationToken);
            metrics["HttpConnectivity"] = httpResult.IsConnected;
            metrics["HttpResponseTimeMs"] = httpResult.ResponseTimeMs;
            metrics["HttpStatusCode"] = httpResult.StatusCode;

            if (!httpResult.IsConnected)
            {
                return ComponentHealth.Unhealthy(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    $"InfluxDB HTTP endpoint not accessible: {httpResult.ErrorMessage}",
                    50,
                    new[]
                    {
                        "Verify InfluxDB is running and accessible",
                        "Check InfluxDB configuration and port settings",
                        "Verify authentication credentials if required",
                        "Review InfluxDB server logs for errors"
                    },
                    metrics);
            }

            if (httpResult.ResponseTimeMs > 5000)
            {
                warnings.Add($"Slow InfluxDB response time: {httpResult.ResponseTimeMs}ms");
                recommendations.Add("Investigate InfluxDB server performance");
            }

            // Test InfluxDB API availability (ping endpoint)
            var apiResult = await TestInfluxDbApi(influxUri, cancellationToken);
            metrics["ApiConnectivity"] = apiResult.IsAvailable;
            metrics["ApiResponseTimeMs"] = apiResult.ResponseTimeMs;
            metrics["InfluxDbVersion"] = apiResult.Version ?? "Unknown";

            if (!apiResult.IsAvailable)
            {
                return ComponentHealth.Unhealthy(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    $"InfluxDB API not responding properly: {apiResult.ErrorMessage}",
                    30,
                    new[]
                    {
                        "Verify InfluxDB service is running correctly",
                        "Check InfluxDB authentication configuration",
                        "Review InfluxDB server logs",
                        "Verify bucket and organization settings"
                    },
                    metrics);
            }

            // Analyze configuration for potential issues
            AnalyzeConfiguration(influxConfig, warnings, recommendations, metrics);

            // Calculate health score
            var healthScore = CalculateHealthScore(metrics, warnings);
            var status = DetermineHealthStatus(healthScore, warnings);

            var statusMessage = status switch
            {
                HealthStatus.Healthy => $"InfluxDB operational (response time: {apiResult.ResponseTimeMs}ms)",
                HealthStatus.Degraded => $"InfluxDB operational with {warnings.Count} warnings",
                HealthStatus.Unhealthy => "InfluxDB has connectivity or performance issues",
                HealthStatus.Critical => "InfluxDB not accessible",
                _ => "Unknown InfluxDB state"
            };

            _logger.LogDebug(
                "InfluxDB health check completed in {Duration}ms. Status: {Status}, Score: {HealthScore}",
                stopwatch.ElapsedMilliseconds, status, healthScore);

            return status switch
            {
                HealthStatus.Healthy => ComponentHealth.Healthy(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    statusMessage,
                    metrics),
                HealthStatus.Degraded => ComponentHealth.Degraded(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    healthScore,
                    statusMessage,
                    warnings,
                    recommendations,
                    metrics),
                _ => ComponentHealth.Unhealthy(
                    "InfluxDB",
                    stopwatch.Elapsed,
                    statusMessage,
                    healthScore,
                    recommendations,
                    metrics)
            };
        }
        catch (Exception ex)
        {
            var errorMessage = _errorService.CreateAndLogError(
                ex,
                "HEALTH-002",
                "InfluxDB health check failed",
                new Dictionary<string, object>
                {
                    ["CheckDuration"] = stopwatch.ElapsedMilliseconds,
                    ["ComponentName"] = "InfluxDB"
                });

            return ComponentHealth.Critical(
                "InfluxDB",
                stopwatch.Elapsed,
                errorMessage.Summary,
                errorMessage.TroubleshootingSteps,
                metrics);
        }
    }

    /// <summary>
    /// Test network connectivity to InfluxDB host
    /// </summary>
    /// <param name="host">InfluxDB host</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Network connectivity result</returns>
    private static async Task<(bool IsConnected, long LatencyMs)> TestNetworkConnectivity(
        string host,
        CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, 5000);

            return reply.Status == IPStatus.Success
                ? (true, reply.RoundtripTime)
                : (false, 0);
        }
        catch
        {
            return (false, 0);
        }
    }

    /// <summary>
    /// Test HTTP connectivity to InfluxDB
    /// </summary>
    /// <param name="influxUri">InfluxDB URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>HTTP connectivity result</returns>
    private async Task<(bool IsConnected, long ResponseTimeMs, int StatusCode, string? ErrorMessage)> TestHttpConnectivity(
        Uri influxUri,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            using var response = await _httpClient.GetAsync(influxUri, cancellationToken);
            stopwatch.Stop();

            return (true, stopwatch.ElapsedMilliseconds, (int)response.StatusCode, null);
        }
        catch (Exception ex)
        {
            return (false, 0, 0, ex.Message);
        }
    }

    /// <summary>
    /// Test InfluxDB API availability
    /// </summary>
    /// <param name="influxUri">InfluxDB URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>API availability result</returns>
    private async Task<(bool IsAvailable, long ResponseTimeMs, string? Version, string? ErrorMessage)> TestInfluxDbApi(
        Uri influxUri,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var pingUri = new Uri(influxUri, "/ping");

            using var response = await _httpClient.GetAsync(pingUri, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var version = response.Headers.TryGetValues("X-Influxdb-Version", out var versionValues)
                    ? versionValues.FirstOrDefault()
                    : null;
                return (true, stopwatch.ElapsedMilliseconds, version, null);
            }

            return (false, stopwatch.ElapsedMilliseconds, null, $"HTTP {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, 0, null, ex.Message);
        }
    }

    /// <summary>
    /// Analyze InfluxDB configuration for potential issues
    /// </summary>
    /// <param name="config">InfluxDB configuration</param>
    /// <param name="warnings">Warnings list</param>
    /// <param name="recommendations">Recommendations list</param>
    /// <param name="metrics">Metrics dictionary</param>
    private static void AnalyzeConfiguration(
        InfluxDbConfig config,
        List<string> warnings,
        List<string> recommendations,
        Dictionary<string, object> metrics)
    {
        // Check batch size
        if (config.WriteBatchSize < 10)
        {
            warnings.Add($"Very small write batch size: {config.WriteBatchSize}");
            recommendations.Add("Consider increasing batch size for better write performance");
        }
        else if (config.WriteBatchSize > 1000)
        {
            warnings.Add($"Very large write batch size: {config.WriteBatchSize}");
            recommendations.Add("Consider decreasing batch size to reduce memory usage");
        }

        // Check flush interval
        if (config.FlushIntervalMs < 1000)
        {
            warnings.Add($"Very short flush interval: {config.FlushIntervalMs}ms");
            recommendations.Add("Consider increasing flush interval to reduce database load");
        }
        else if (config.FlushIntervalMs > 60000)
        {
            warnings.Add($"Very long flush interval: {config.FlushIntervalMs}ms");
            recommendations.Add("Consider decreasing flush interval for better data freshness");
        }

        // Check for empty required fields
        if (string.IsNullOrEmpty(config.Bucket))
        {
            warnings.Add("InfluxDB bucket not configured");
            recommendations.Add("Configure InfluxDB bucket name");
        }

        if (string.IsNullOrEmpty(config.Organization))
        {
            warnings.Add("InfluxDB organization not configured");
            recommendations.Add("Configure InfluxDB organization name");
        }

        metrics["BatchSizeValid"] = config.WriteBatchSize >= 10 && config.WriteBatchSize <= 1000;
        metrics["FlushIntervalValid"] = config.FlushIntervalMs >= 1000 && config.FlushIntervalMs <= 60000;
        metrics["BucketConfigured"] = !string.IsNullOrEmpty(config.Bucket);
        metrics["OrganizationConfigured"] = !string.IsNullOrEmpty(config.Organization);
    }

    /// <summary>
    /// Calculate health score based on metrics and warnings
    /// </summary>
    /// <param name="metrics">Current metrics</param>
    /// <param name="warnings">Current warnings</param>
    /// <returns>Health score (0-100)</returns>
    private static int CalculateHealthScore(Dictionary<string, object> metrics, List<string> warnings)
    {
        var baseScore = 100;

        // Penalize for warnings
        baseScore -= warnings.Count * 10;

        // Network latency penalties
        if (metrics.TryGetValue("NetworkLatencyMs", out var latencyObj) && latencyObj is long latency)
        {
            if (latency > 1000)
                baseScore -= 20;
            else if (latency > 500)
                baseScore -= 10;
        }

        // HTTP response time penalties
        if (metrics.TryGetValue("HttpResponseTimeMs", out var responseTimeObj) && responseTimeObj is long responseTime)
        {
            if (responseTime > 5000)
                baseScore -= 20;
            else if (responseTime > 2000)
                baseScore -= 10;
        }

        // API response time penalties
        if (metrics.TryGetValue("ApiResponseTimeMs", out var apiTimeObj) && apiTimeObj is long apiTime)
        {
            if (apiTime > 3000)
                baseScore -= 15;
            else if (apiTime > 1000)
                baseScore -= 5;
        }

        return Math.Max(0, Math.Min(100, baseScore));
    }

    /// <summary>
    /// Determine health status based on score and warnings
    /// </summary>
    /// <param name="healthScore">Current health score</param>
    /// <param name="warnings">Current warnings</param>
    /// <returns>Health status</returns>
    private static HealthStatus DetermineHealthStatus(int healthScore, List<string> warnings)
    {
        if (healthScore >= 90 && warnings.Count == 0)
            return HealthStatus.Healthy;

        if (healthScore >= 70 && warnings.Count <= 2)
            return HealthStatus.Degraded;

        if (healthScore >= 30)
            return HealthStatus.Unhealthy;

        return HealthStatus.Critical;
    }
}
