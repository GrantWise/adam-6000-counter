# Health Monitoring System

The Industrial ADAM Logger includes a comprehensive health monitoring system that follows established architectural patterns and remains framework-agnostic.

## Core Components

### IHealthCheckService
The main interface for performing health checks on system components:

```csharp
public interface IHealthCheckService
{
    Task<OperationResult<HealthResponse>> CheckHealthAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<ComponentHealth>> CheckComponentHealthAsync(string componentName, CancellationToken cancellationToken = default);
    Task<OperationResult<HealthStatus>> GetQuickHealthStatusAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<IReadOnlyDictionary<string, object>>> GetHealthMetricsAsync(CancellationToken cancellationToken = default);
}
```

### Built-in Health Checks

1. **ApplicationHealthCheck** - Monitors application performance, memory usage, and system resources
2. **InfluxDbHealthCheck** - Validates InfluxDB connectivity and performance  
3. **SystemResourceHealthCheck** - Monitors CPU, memory, disk, and .NET runtime metrics

## Usage

### Basic Registration

```csharp
services.AddAdamLoggerHealthMonitoring();
```

### Complete Setup with All Features

```csharp
services.AddAdamLoggerComplete(configuration);
```

### Manual Health Check

```csharp
public class MyService
{
    private readonly IHealthCheckService _healthService;
    
    public MyService(IHealthCheckService healthService)
    {
        _healthService = healthService;
    }
    
    public async Task<HealthResponse> CheckSystemHealthAsync()
    {
        var result = await _healthService.CheckHealthAsync();
        return result.IsSuccess ? result.Value : throw new InvalidOperationException(result.ErrorMessage);
    }
}
```

### Custom Health Checks

```csharp
// Register a custom health check
healthService.RegisterComponentHealthCheck("MyComponent", async (cancellationToken) =>
{
    // Your health check logic here
    if (await IsMyComponentHealthyAsync())
    {
        return ComponentHealth.Healthy("MyComponent", TimeSpan.FromMilliseconds(100));
    }
    else
    {
        return ComponentHealth.Unhealthy("MyComponent", TimeSpan.FromMilliseconds(100), "Component is down");
    }
});
```

## Integration with ASP.NET Core

For ASP.NET Core applications, you can create HTTP endpoints that expose the health information:

```csharp
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IHealthCheckService _healthService;
    
    public HealthController(IHealthCheckService healthService)
    {
        _healthService = healthService;
    }
    
    [HttpGet]
    public async Task<ActionResult<HealthResponse>> GetHealth()
    {
        var result = await _healthService.CheckHealthAsync();
        if (!result.IsSuccess)
        {
            return StatusCode(503, new { error = result.ErrorMessage });
        }
        
        var health = result.Value;
        var statusCode = health.Status switch
        {
            HealthStatus.Healthy => 200,
            HealthStatus.Degraded => 200,
            _ => 503
        };
        
        return StatusCode(statusCode, health);
    }
    
    [HttpGet("status")]
    public async Task<ActionResult> GetQuickStatus()
    {
        var result = await _healthService.GetQuickHealthStatusAsync();
        if (!result.IsSuccess)
        {
            return StatusCode(503, new { status = "unhealthy" });
        }
        
        var isHealthy = result.Value == HealthStatus.Healthy || result.Value == HealthStatus.Degraded;
        return StatusCode(isHealthy ? 200 : 503, new { status = result.Value.ToString().ToLowerInvariant() });
    }
}
```

## Integration with .NET Health Checks

The library integrates with the standard .NET health check system:

```csharp
services.AddHealthChecks()
    .AddCheck<AdamLoggerService>("adam_logger");
```

## Kubernetes Integration

For Kubernetes deployments, you can create liveness and readiness probes:

```csharp
// Startup.cs or Program.cs
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

## Metrics Export

The health system provides detailed metrics that can be exported to monitoring systems:

```csharp
var metricsResult = await healthService.GetHealthMetricsAsync();
if (metricsResult.IsSuccess)
{
    // Export to Prometheus, Grafana, etc.
    foreach (var metric in metricsResult.Value)
    {
        // Process metric
    }
}
```

## Framework Agnostic Design

The health monitoring system is designed to work with any .NET application type:

- Console applications
- Windows Services  
- ASP.NET Core web applications
- Blazor applications
- Azure Functions
- Docker containers

The core library provides the health checking logic, while consuming applications decide how to expose or use the health information.