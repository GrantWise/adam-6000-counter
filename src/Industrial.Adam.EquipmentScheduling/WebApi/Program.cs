using System.Reflection;
using System.Text.Json.Serialization;
using Industrial.Adam.EquipmentScheduling.Application;
using Industrial.Adam.EquipmentScheduling.Domain;
using Industrial.Adam.EquipmentScheduling.Infrastructure;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("ApplicationName", "Equipment.Scheduling.WebApi")
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine("logs", "equipment-scheduling-webapi-.txt"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

// Add services to the container
builder.Services.AddControllers(options =>
{
    // Configure model binding and validation
    options.SuppressAsyncSuffixInActionNames = false;
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.AddJsonOptions(options =>
{
    // Configure JSON serialization
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep original property names
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Problem Details middleware
builder.Services.AddProblemDetails();

// API Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Equipment Scheduling API",
        Version = "v1",
        Description = "RESTful API for managing equipment scheduling and availability",
        Contact = new()
        {
            Name = "Industrial Systems Team",
            Email = "support@industrialsystems.com"
        }
    });

    // Include XML documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Include Application layer XML documentation
    var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, "Industrial.Adam.EquipmentScheduling.Application.xml");
    if (File.Exists(applicationXmlPath))
    {
        options.IncludeXmlComments(applicationXmlPath);
    }

    // Configure schema generation
    options.UseAllOfToExtendReferenceSchemas();
    options.SupportNonNullableReferenceTypes();
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Application Services
builder.Services.AddEquipmentSchedulingDomain();
builder.Services.AddEquipmentSchedulingApplication();
builder.Services.AddEquipmentSchedulingInfrastructure(builder.Configuration, builder.Environment);

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Equipment Scheduling API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
        options.DisplayRequestDuration();
        options.EnableDeepLinking();
        options.EnableFilter();
        options.ShowExtensions();
        options.EnableValidator();
    });

    // Ensure database is created in development
    try
    {
        await app.Services.EnsureDatabaseAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to ensure database during startup");
        throw;
    }
}
else
{
    app.UseExceptionHandler();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
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
                duration = x.Value.Duration.ToString(),
                exception = x.Value.Exception?.Message,
                data = x.Value.Data
            }),
            totalDuration = report.TotalDuration.ToString()
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

app.MapGet("/", () => new
{
    service = "Equipment Scheduling API",
    version = "1.0.0",
    environment = app.Environment.EnvironmentName,
    timestamp = DateTime.UtcNow,
    endpoints = new[]
    {
        "/swagger - API Documentation",
        "/health - Health Check",
        "/api/equipment-scheduling/resources - Resource Management",
        "/api/equipment-scheduling/availability - Equipment Availability"
    }
});

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(() =>
{
    app.Logger.LogInformation("Equipment Scheduling API is shutting down...");
});

try
{
    app.Logger.LogInformation("Starting Equipment Scheduling API");
    await app.RunAsync();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Equipment Scheduling API terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible to test projects
public partial class Program { }
