using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Industrial.Adam.AdminDashboard.Domain.ValueObjects;
using Industrial.Adam.AdminDashboard.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net.NetworkInformation;
using Npgsql;
using Dapper;

namespace Industrial.Adam.AdminDashboard.Infrastructure.Services;

/// <summary>
/// Implementation of system health monitoring service
/// </summary>
public class SystemHealthService : ISystemHealthService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SystemHealthService> _logger;
    private readonly string _connectionString;

    private readonly Dictionary<string, string> _serviceEndpoints = new()
    {
        { "OEE-API", "http://localhost:5001/health" },
        { "Logger-API", "http://localhost:5002/health" },
        { "Security-API", "http://localhost:5003/health" },
        { "AdminDashboard-API", "http://localhost:5004/health" }
    };

    /// <summary>
    /// Constructor for SystemHealthService
    /// </summary>
    /// <param name="configuration">Configuration instance</param>
    /// <param name="logger">Logger instance</param>
    public SystemHealthService(
        IConfiguration configuration,
        ILogger<SystemHealthService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _connectionString = GetConnectionString(configuration);
    }

    /// <summary>
    /// Check health of all registered services
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of service health results</returns>
    public async Task<IEnumerable<ServiceHealthResult>> CheckAllServicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking health of all services");

        var healthTasks = _serviceEndpoints.Select(async kvp => 
        {
            try
            {
                return await CheckServiceHealthAsync(kvp.Key, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for service {ServiceName}", kvp.Key);
                return ServiceHealthResult.Unhealthy(kvp.Key, ServiceStatus.Critical, $"Health check failed: {ex.Message}");
            }
        });

        var results = await Task.WhenAll(healthTasks);
        
        _logger.LogInformation("Completed health check for {ServiceCount} services", results.Length);
        
        return results;
    }

    /// <summary>
    /// Check health of a specific service
    /// </summary>
    /// <param name="serviceName">Name of the service to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Service health result</returns>
    public async Task<ServiceHealthResult> CheckServiceHealthAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        if (!_serviceEndpoints.TryGetValue(serviceName, out var endpoint))
        {
            return ServiceHealthResult.Unhealthy(serviceName, ServiceStatus.Unknown, "Service endpoint not configured");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync(endpoint, cancellationToken);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var responseTime = (int)stopwatch.ElapsedMilliseconds;
                var status = responseTime > 1000 ? ServiceStatus.Warning : ServiceStatus.Healthy;
                
                return ServiceHealthResult.Healthy(serviceName, responseTime, new Dictionary<string, object>
                {
                    { "StatusCode", (int)response.StatusCode },
                    { "Endpoint", endpoint }
                });
            }
            else
            {
                return ServiceHealthResult.Unhealthy(serviceName, ServiceStatus.Critical, 
                    $"HTTP {response.StatusCode}", 1, (int)stopwatch.ElapsedMilliseconds);
            }
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            return ServiceHealthResult.Unhealthy(serviceName, ServiceStatus.Critical, "Request timeout", 1, (int)stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ServiceHealthResult.Unhealthy(serviceName, ServiceStatus.Critical, ex.Message, 1, (int)stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Get system performance metrics
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>System metrics</returns>
    public async Task<SystemMetrics> GetSystemMetricsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Collecting system performance metrics");

        var tasks = new Task[]
        {
            Task.Run(GetCpuUsageAsync, cancellationToken),
            Task.Run(GetMemoryUsageAsync, cancellationToken),
            Task.Run(GetDiskUsageAsync, cancellationToken)
        };

        await Task.WhenAll(tasks);

        var cpuUsage = await (tasks[0] as Task<double>)!;
        var memoryInfo = await (tasks[1] as Task<(long Used, long Total)>)!;
        var diskInfo = await (tasks[2] as Task<(long Used, long Total)>)!;

        return new SystemMetrics
        {
            CpuUsagePercent = cpuUsage,
            MemoryUsageBytes = memoryInfo.Used,
            TotalMemoryBytes = memoryInfo.Total,
            DiskUsageBytes = diskInfo.Used,
            TotalDiskBytes = diskInfo.Total,
            UptimeSeconds = Environment.TickCount64 / 1000,
            Timestamp = DateTime.UtcNow,
            AdditionalMetrics = new Dictionary<string, object>
            {
                { "ProcessCount", Process.GetProcesses().Length },
                { "ThreadCount", Process.GetCurrentProcess().Threads.Count },
                { "GcMemory", GC.GetTotalMemory(false) }
            }
        };
    }

    /// <summary>
    /// Get database health information
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Database health result</returns>
    public async Task<DatabaseHealthResult> GetDatabaseHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Test query to check database responsiveness
            var version = await connection.QuerySingleAsync<string>("SELECT version()");
            
            // Get connection stats using proper parameterized queries
            var connectionStats = await connection.QuerySingleAsync(@"
                SELECT 
                    (SELECT setting::int FROM pg_settings WHERE name = 'max_connections') as max_connections,
                    (SELECT count(*) FROM pg_stat_activity WHERE state = 'active') as active_connections,
                    (SELECT pg_size_pretty(pg_database_size(current_database()))) as database_size");

            stopwatch.Stop();

            return DatabaseHealthResult.Healthy(
                responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                version: version,
                activeConnections: connectionStats.active_connections,
                maxConnections: connectionStats.max_connections,
                additionalMetrics: new Dictionary<string, object>
                {
                    { "DatabaseSize", connectionStats.database_size },
                    { "ConnectionString", _connectionString.Split(';').FirstOrDefault(x => x.StartsWith("Host=")) ?? "Unknown" }
                }
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Database health check failed");
            return DatabaseHealthResult.Unhealthy(ServiceStatus.Critical, ex.Message, (int)stopwatch.ElapsedMilliseconds);
        }
    }

    private static async Task<double> GetCpuUsageAsync()
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            var startTime = DateTime.UtcNow;
            var startCpuUsage = process.TotalProcessorTime;
            
            await Task.Delay(1000);
            
            var endTime = DateTime.UtcNow;
            var endCpuUsage = process.TotalProcessorTime;
            
            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;
            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);
            
            return cpuUsageTotal * 100;
        }
        catch
        {
            return 0;
        }
    }

    private static async Task<(long Used, long Total)> GetMemoryUsageAsync()
    {
        try
        {
            await Task.Yield();
            var process = Process.GetCurrentProcess();
            var used = process.WorkingSet64;
            
            // Approximate total system memory (this is a simplification)
            var total = used * 4; // Assume we're using ~25% of available memory as baseline
            
            return (used, total);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static async Task<(long Used, long Total)> GetDiskUsageAsync()
    {
        try
        {
            await Task.Yield();
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed);
            
            long totalUsed = 0;
            long totalSize = 0;
            
            foreach (var drive in drives)
            {
                totalUsed += drive.TotalSize - drive.AvailableFreeSpace;
                totalSize += drive.TotalSize;
            }
            
            return (totalUsed, totalSize);
        }
        catch
        {
            return (0, 0);
        }
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration.GetConnectionString("TimescaleDB")
            ?? BuildConnectionStringFromEnvironment();

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("No TimescaleDB connection string found in configuration");
        }

        return connectionString;
    }

    private static string BuildConnectionStringFromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("TIMESCALE_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("TIMESCALE_PORT") ?? "5433";
        var database = Environment.GetEnvironmentVariable("TIMESCALE_DATABASE") ?? "adam_counters";
        var username = Environment.GetEnvironmentVariable("TIMESCALE_USERNAME") ?? "industrial_system";
        var password = Environment.GetEnvironmentVariable("TIMESCALE_PASSWORD") ?? Environment.GetEnvironmentVariable("TIMESCALEDB_PASSWORD");

        if (string.IsNullOrEmpty(password))
        {
            return string.Empty;
        }

        return $"Host={host};Port={port};Database={database};Username={username};Password={password};Include Error Detail=true";
    }
}

