using System.Reflection;
using System.Text.Json.Serialization;
using Industrial.Adam.EquipmentScheduling.Application;
using Industrial.Adam.EquipmentScheduling.Domain;
using Industrial.Adam.EquipmentScheduling.Infrastructure;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Configuration;
using Industrial.Adam.Security.Authentication;
using Industrial.Adam.Security.Extensions;
using Industrial.Adam.Security.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add environment file support
builder.Configuration.AddEnvironmentFiles(builder.Environment.EnvironmentName);

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

    // JWT Authentication configuration
    options.AddSecurityDefinition("Bearer", new()
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token to access protected endpoints"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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

// Add Industrial Adam security (JWT authentication + secure CORS)
builder.Services.AddIndustrialAdamSecurity(builder.Configuration);

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

// Use secure CORS policy
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "SecurePolicy");

app.UseRouting();

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add authentication endpoints
app.MapPost("/auth/login", async (AuthenticationRequest request, JwtAuthenticationService authService) =>
{
    var response = await authService.AuthenticateAsync(request);

    if (response == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(response);
})
.AllowAnonymous();

app.MapPost("/auth/refresh", async (RefreshTokenRequest request, JwtAuthenticationService authService) =>
{
    var response = await authService.RefreshTokenAsync(request);

    if (response == null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(response);
})
.AllowAnonymous();

app.MapPost("/auth/logout", async (RefreshTokenRequest request, JwtAuthenticationService authService) =>
{
    await authService.RevokeTokenAsync(request.RefreshToken);
    return Results.Ok(new { Message = "Logged out successfully" });
})
.RequireAuthorization();

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

/// <summary>
/// Equipment Scheduling WebAPI application entry point
/// </summary>
public partial class Program { }
