using System.Collections.Concurrent;
using Industrial.Adam.Logger.Core.Devices;
using Industrial.Adam.Logger.Core.Extensions;
using Industrial.Adam.Logger.Core.Models;
using Industrial.Adam.Logger.Core.Services;
using Industrial.Adam.Logger.Core.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ADAM Industrial Logger API",
        Version = "v1",
        Description = "Minimal API for ADAM-6051 device monitoring and data collection"
    });
});

// Add CORS for development and production
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add ADAM Logger Core services
builder.Services.AddAdamLogger(builder.Configuration);

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<InfluxDbHealthCheck>("influxdb")
    .AddCheck<DevicePoolHealthCheck>("device-pool");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ADAM Logger API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

// Cache for latest readings
var latestReadings = new ConcurrentDictionary<string, DeviceReading>();

// Subscribe to device readings on startup
var devicePool = app.Services.GetRequiredService<IServiceProvider>().GetService<ModbusDevicePool>();
if (devicePool != null)
{
    devicePool.ReadingReceived += (reading) =>
    {
        var key = $"{reading.DeviceId}:{reading.Channel}";
        latestReadings.AddOrUpdate(key, reading, (k, existing) => reading);
    };
}

// ============================================================================
// HEALTH ENDPOINTS
// ============================================================================

app.MapGet("/health", (AdamLoggerService loggerService) =>
{
    var status = loggerService.GetStatus();
    var result = new
    {
        Status = status.IsRunning ? "Healthy" : "Unhealthy",
        Timestamp = DateTimeOffset.UtcNow,
        Service = new
        {
            IsRunning = status.IsRunning,
            StartTime = status.StartTime,
            Uptime = status.IsRunning ? DateTimeOffset.UtcNow - status.StartTime : TimeSpan.Zero
        },
        Devices = new
        {
            Total = status.TotalDevices,
            Connected = status.ConnectedDevices,
            Health = status.DeviceHealth
        }
    };

    return Results.Ok(result);
})
.WithName("GetHealth");

app.MapGet("/health/detailed", async (AdamLoggerService loggerService, IInfluxDbStorage influxStorage) =>
{
    var status = loggerService.GetStatus();
    var influxHealthy = await influxStorage.TestConnectionAsync();

    var result = new
    {
        Status = status.IsRunning && influxHealthy ? "Healthy" : "Unhealthy",
        Timestamp = DateTimeOffset.UtcNow,
        Components = new
        {
            Service = new
            {
                Status = status.IsRunning ? "Healthy" : "Unhealthy",
                IsRunning = status.IsRunning,
                StartTime = status.StartTime,
                Uptime = status.IsRunning ? DateTimeOffset.UtcNow - status.StartTime : TimeSpan.Zero
            },
            Database = new
            {
                Status = influxHealthy ? "Healthy" : "Unhealthy",
                Connected = influxHealthy
            },
            Devices = new
            {
                Status = status.ConnectedDevices == status.TotalDevices ? "Healthy" :
                        status.ConnectedDevices > 0 ? "Degraded" : "Unhealthy",
                Total = status.TotalDevices,
                Connected = status.ConnectedDevices,
                Details = status.DeviceHealth
            }
        }
    };

    return Results.Ok(result);
})
.WithName("GetDetailedHealth");

// ============================================================================
// DEVICE ENDPOINTS  
// ============================================================================

app.MapGet("/devices", (AdamLoggerService loggerService) =>
{
    var status = loggerService.GetStatus();
    return Results.Ok(status.DeviceHealth);
})
.WithName("GetDevices");

app.MapGet("/devices/{deviceId}", (string deviceId, AdamLoggerService loggerService) =>
{
    var status = loggerService.GetStatus();
    if (status.DeviceHealth.TryGetValue(deviceId, out var health))
    {
        return Results.Ok(health);
    }

    return Results.NotFound(new { Error = $"Device '{deviceId}' not found" });
})
.WithName("GetDevice");

app.MapPost("/devices/{deviceId}/restart", async (string deviceId, AdamLoggerService loggerService) =>
{
    try
    {
        var result = await loggerService.RestartDeviceAsync(deviceId);
        if (result)
        {
            return Results.Ok(new { Message = $"Device '{deviceId}' restarted successfully" });
        }

        return Results.NotFound(new { Error = $"Device '{deviceId}' not found" });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            detail: ex.Message,
            title: "Device restart failed",
            statusCode: 500);
    }
})
.WithName("RestartDevice");

// ============================================================================
// DATA ENDPOINTS
// ============================================================================

app.MapGet("/data/latest", () =>
{
    var readings = latestReadings.Values
        .OrderBy(r => r.DeviceId)
        .ThenBy(r => r.Channel)
        .ToList();

    return Results.Ok(new
    {
        Count = readings.Count,
        LastUpdated = readings.Count > 0 ? readings.Max(r => r.Timestamp) : (DateTimeOffset?)null,
        Readings = readings
    });
})
.WithName("GetLatestData");

app.MapGet("/data/latest/{deviceId}", (string deviceId) =>
{
    var deviceReadings = latestReadings.Values
        .Where(r => r.DeviceId.Equals(deviceId, StringComparison.OrdinalIgnoreCase))
        .OrderBy(r => r.Channel)
        .ToList();

    if (deviceReadings.Count == 0)
    {
        return Results.NotFound(new { Error = $"No readings found for device '{deviceId}'" });
    }

    return Results.Ok(new
    {
        DeviceId = deviceId,
        Count = deviceReadings.Count,
        LastUpdated = deviceReadings.Max(r => r.Timestamp),
        Readings = deviceReadings
    });
})
.WithName("GetDeviceLatestData");

app.MapGet("/data/stats", (AdamLoggerService loggerService) =>
{
    var status = loggerService.GetStatus();
    var readings = latestReadings.Values.ToList();

    var deviceStats = readings.GroupBy(r => r.DeviceId)
        .Select(g => new
        {
            DeviceId = g.Key,
            ChannelCount = g.Count(),
            LastUpdate = g.Max(r => r.Timestamp),
            AverageRate = g.Where(r => r.Rate.HasValue).DefaultIfEmpty().Average(r => r?.Rate ?? 0),
            QualityDistribution = g.GroupBy(r => r.Quality)
                .ToDictionary(q => q.Key.ToString(), q => q.Count())
        }).ToList();

    return Results.Ok(new
    {
        Summary = new
        {
            ServiceRunning = status.IsRunning,
            ServiceUptime = status.IsRunning ? DateTimeOffset.UtcNow - status.StartTime : TimeSpan.Zero,
            TotalDevices = status.TotalDevices,
            ConnectedDevices = status.ConnectedDevices,
            TotalReadings = readings.Count,
            LastDataUpdate = readings.Count > 0 ? readings.Max(r => r.Timestamp) : (DateTimeOffset?)null
        },
        DeviceStatistics = deviceStats
    });
})
.WithName("GetDataStatistics");

// ============================================================================
// CONFIGURATION ENDPOINTS
// ============================================================================

app.MapGet("/config", (IConfiguration configuration) =>
{
    // Return safe configuration info (no secrets)
    var safeConfig = new
    {
        Environment = app.Environment.EnvironmentName,
        LogLevel = configuration["Logging:LogLevel:Default"],
        DemoMode = configuration.GetValue<bool>("DemoMode"),
        InfluxDb = new
        {
            Url = configuration["InfluxDb:Url"],
            Organization = configuration["InfluxDb:Organization"],
            Bucket = configuration["InfluxDb:Bucket"],
            BatchSize = configuration.GetValue<int>("InfluxDb:BatchSize"),
            FlushIntervalMs = configuration.GetValue<int>("InfluxDb:FlushIntervalMs")
        }
    };

    return Results.Ok(safeConfig);
})
.WithName("GetConfiguration");

// ============================================================================
// UTILITY ENDPOINTS
// ============================================================================

app.MapDelete("/data/cache", () =>
{
    var count = latestReadings.Count;
    latestReadings.Clear();

    return Results.Ok(new { Message = $"Cleared {count} cached readings" });
})
.WithName("ClearDataCache");

// Add built-in health checks endpoint
app.MapHealthChecks("/health/checks");

app.Run();

// ============================================================================
// HEALTH CHECK IMPLEMENTATIONS
// ============================================================================

/// <summary>
/// Health check for InfluxDB connectivity
/// </summary>
public class InfluxDbHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly IInfluxDbStorage _storage;

    /// <summary>
    /// Initialize InfluxDB health check
    /// </summary>
    /// <param name="storage">InfluxDB storage instance</param>
    public InfluxDbHealthCheck(IInfluxDbStorage storage)
    {
        _storage = storage;
    }

    /// <summary>
    /// Check InfluxDB connection health
    /// </summary>
    public async Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connected = await _storage.TestConnectionAsync(cancellationToken);
            return connected
                ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("InfluxDB connection is healthy")
                : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("InfluxDB connection failed");
        }
        catch (Exception ex)
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"InfluxDB check failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Health check for device pool connectivity
/// </summary>
public class DevicePoolHealthCheck : Microsoft.Extensions.Diagnostics.HealthChecks.IHealthCheck
{
    private readonly AdamLoggerService _service;

    /// <summary>
    /// Initialize device pool health check
    /// </summary>
    /// <param name="service">ADAM logger service instance</param>
    public DevicePoolHealthCheck(AdamLoggerService service)
    {
        _service = service;
    }

    /// <summary>
    /// Check device pool connection health
    /// </summary>
    public Task<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult> CheckHealthAsync(
        Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _service.GetStatus();
        var description = $"{status.ConnectedDevices}/{status.TotalDevices} devices connected";

        if (status.ConnectedDevices == 0 && status.TotalDevices > 0)
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy(description));
        }
        else if (status.ConnectedDevices < status.TotalDevices)
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded(description));
        }
        else
        {
            return Task.FromResult(Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(description));
        }
    }
}
