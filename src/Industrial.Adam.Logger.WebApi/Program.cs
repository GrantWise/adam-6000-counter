using Industrial.Adam.Logger.Extensions;
using Industrial.Adam.Logger.Logging;
using Industrial.Adam.Logger.WebApi.Hubs;
using Industrial.Adam.Logger.WebApi.Middleware;
using Industrial.Adam.Logger.WebApi.Services;
using Microsoft.OpenApi.Models;
using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ADAM Counter Logger API",
        Description = "REST API for ADAM-6000 series counter device management and monitoring",
        Contact = new OpenApiContact
        {
            Name = "Industrial Systems Team",
            Email = "support@industrial.com"
        }
    });

    // Include XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Configure CORS for frontend access
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",     // Next.js development
                "http://localhost:3001",     // Alternative port
                "http://localhost:5173"      // Vite development
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Add AutoMapper
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

// Add FluentValidation (modern approach)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add ADAM Logger services
builder.Services.AddAdamLoggerWithStructuredLogging(builder.Configuration, config =>
{
    // Allow configuration override from appsettings.json
    builder.Configuration.GetSection("AdamLogger").Bind(config);
});

// Add API-specific services
builder.Services.AddScoped<IDeviceOrchestrator, DeviceOrchestrator>();
builder.Services.AddHostedService<RealtimeDataService>();

// Configure JSON serialization options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Global error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// Enable CORS
app.UseCors("FrontendPolicy");

// Swagger UI - available in all environments for this industrial application
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ADAM Counter Logger API v1");
    options.RoutePrefix = "api-docs";
    options.DocumentTitle = "ADAM Counter Logger API Documentation";
});

// Add a redirect from root to Swagger UI
app.MapGet("/", () => Results.Redirect("/api-docs")).ExcludeFromDescription();

app.UseAuthorization();

app.MapControllers();

// Map SignalR hubs
app.MapHub<CounterDataHub>("/hubs/counter-data");
app.MapHub<HealthStatusHub>("/hubs/health-status");

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("ADAM Counter Logger Web API started");
logger.LogInformation("Swagger UI available at: {SwaggerUrl}", $"{app.Urls.FirstOrDefault()}/api-docs");

app.Run();