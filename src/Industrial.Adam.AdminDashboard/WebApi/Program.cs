using System.Reflection;
using Industrial.Adam.AdminDashboard.Application;
using Industrial.Adam.AdminDashboard.Domain;
using Industrial.Adam.AdminDashboard.Infrastructure;
using Industrial.Adam.AdminDashboard.Infrastructure.SignalR;
using Industrial.Adam.Security.Authentication;
using Industrial.Adam.Security.Extensions;
using Industrial.Adam.Security.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Add environment file support
builder.Configuration.AddEnvironmentFiles(builder.Environment.EnvironmentName);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = false;
    options.SuppressMapClientErrors = false;
});

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Admin Dashboard API",
        Version = "v1",
        Description = "System monitoring and administration API for Industrial ADAM platform",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Industrial Systems Team"
        }
    });

    // JWT Authentication configuration
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter JWT Bearer token to access protected endpoints"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Add Industrial Adam security (JWT authentication + secure CORS)
builder.Services.AddIndustrialAdamSecurity(builder.Configuration);

// Add AdminDashboard layers
builder.Services.AddAdminDashboardDomain();
builder.Services.AddAdminDashboardApplication();
builder.Services.AddAdminDashboardInfrastructure(builder.Configuration);

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy("AdminDashboard API is running"))
    .AddCheck("mediator", () => HealthCheckResult.Healthy("MediatR services registered"))
    .AddCheck("application_layer", () => HealthCheckResult.Healthy("Application layer services registered"))
    .AddCheck("infrastructure_layer", () => HealthCheckResult.Healthy("Infrastructure layer services registered"));

var app = builder.Build();

// Configure the HTTP request pipeline

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin Dashboard API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
        c.DisplayRequestDuration();
        c.EnableTryItOutByDefault();
    });
}
else
{
    // Add security headers for production
    app.Use((context, next) =>
    {
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        return next();
    });
}

// Add request logging
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? "unknown");
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
    };
});

app.UseHttpsRedirection();

// Use secure CORS policy
app.UseCors(app.Environment.IsDevelopment() ? "Development" : "SecurePolicy");

// Add authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Add SignalR hub
app.MapHub<HealthUpdateHub>("/healthHub");

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

// Add Health Check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/detailed", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});

try
{
    Log.Information("Starting AdminDashboard API application on {Environment}", app.Environment.EnvironmentName);
    Log.Information("Application listening on {Urls}", string.Join(", ", app.Urls));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AdminDashboard API application terminated unexpectedly");
    throw; // Re-throw for proper exit code
}
finally
{
    Log.Information("AdminDashboard API application shutting down");
    Log.CloseAndFlush();
}

/// <summary>
/// AdminDashboard API application entry point and configuration
/// </summary>
public partial class Program { }