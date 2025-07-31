using Industrial.Adam.Logger.Core.Extensions;
using Industrial.Adam.Logger.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ADAM Logger API", Version = "v1" });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<InfluxDbHealthCheck>("influxdb")
    .AddTypeActivatedCheck<DevicePoolHealthCheck>("device-pool");

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add ADAM Logger Core services
builder.Services.AddAdamLogger(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Development");
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

// Map health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
});

app.Run();

// Health check implementations
public class InfluxDbHealthCheck : IHealthCheck
{
    private readonly Industrial.Adam.Logger.Core.Storage.IInfluxDbStorage _storage;
    
    public InfluxDbHealthCheck(Industrial.Adam.Logger.Core.Storage.IInfluxDbStorage storage)
    {
        _storage = storage;
    }
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connected = await _storage.TestConnectionAsync(cancellationToken);
            return connected
                ? HealthCheckResult.Healthy("InfluxDB connection is healthy")
                : HealthCheckResult.Unhealthy("InfluxDB connection failed");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"InfluxDB check failed: {ex.Message}");
        }
    }
}

public class DevicePoolHealthCheck : IHealthCheck
{
    private readonly Industrial.Adam.Logger.Core.Services.AdamLoggerService _service;
    
    public DevicePoolHealthCheck(Industrial.Adam.Logger.Core.Services.AdamLoggerService service)
    {
        _service = service;
    }
    
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _service.GetStatus();
        var description = $"{status.ConnectedDevices}/{status.TotalDevices} devices connected";
        
        if (status.ConnectedDevices == 0 && status.TotalDevices > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(description));
        }
        else if (status.ConnectedDevices < status.TotalDevices)
        {
            return Task.FromResult(HealthCheckResult.Degraded(description));
        }
        else
        {
            return Task.FromResult(HealthCheckResult.Healthy(description));
        }
    }
}

public static class HealthCheckResponseWriter
{
    public static Task WriteResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        
        return context.Response.WriteAsJsonAsync(response);
    }
}