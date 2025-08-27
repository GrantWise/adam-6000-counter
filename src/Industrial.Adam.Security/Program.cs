using Industrial.Adam.Security.Infrastructure.Services;
using Industrial.Adam.Security.Domain.Constants;
using Industrial.Adam.Security.Infrastructure.Extensions;
using Industrial.Adam.Security.Hubs;
using Industrial.Adam.Security.Application.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Industrial ADAM Security API", 
        Version = "v1",
        Description = "Security and admin dashboard API for Phase 2"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme.",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // React dev servers
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // For SignalR
    });
});

// Configure JWT Authentication
var jwtKey = builder.Configuration["JWT_SECRET_KEY"] 
    ?? throw new InvalidOperationException("JWT_SECRET_KEY must be configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // For development
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT_ISSUER"] ?? "Industrial.Adam.System",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT_AUDIENCE"] ?? "Industrial.Adam.APIs",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/ws"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Configure Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireSystemAdmin", policy =>
        policy.RequireClaim(ClaimTypes.Role, RoleConstants.SystemAdmin));
    
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireClaim(ClaimTypes.Role, RoleConstants.SystemAdmin, RoleConstants.Admin));
});

// Add Security Services
builder.Services.AddSingleton<JwtAuthenticationService>();
builder.Services.AddSingleton<UserStorageService>();

// Add Phase 2 Admin Dashboard Services
builder.Services.AddAdminDashboardPhase2Services(builder.Configuration);

// Add comprehensive security
builder.Services.AddComprehensiveSecurity(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Industrial ADAM Security API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseSerilogRequestLogging();

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

// Use comprehensive security pipeline
app.UseComprehensiveSecurityPipeline(builder.Configuration);

app.MapControllers();

// Map SignalR hubs
app.MapHub<SecurityEventsHub>("/ws/security-events");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { 
    status = "Healthy", 
    timestamp = DateTimeOffset.UtcNow,
    version = "2.0.0-Phase2"
}));

// Authentication endpoints with strict rate limiting
app.MapPost("/auth/login", async (
    AuthenticationRequest request,
    JwtAuthenticationService authService) =>
{
    var response = await authService.AuthenticateAsync(request);
    return response != null ? Results.Ok(response) : Results.Unauthorized();
}).RequireRateLimiting("AuthPolicy");

app.MapPost("/auth/refresh", async (
    RefreshTokenRequest request,
    JwtAuthenticationService authService) =>
{
    var response = await authService.RefreshTokenAsync(request);
    return response != null ? Results.Ok(response) : Results.Unauthorized();
}).RequireRateLimiting("AuthPolicy");

app.MapPost("/auth/logout", async (
    RefreshTokenRequest request,
    JwtAuthenticationService authService) =>
{
    var success = await authService.RevokeTokenAsync(request.RefreshToken);
    return Results.Ok(new { message = "Logged out successfully" });
}).RequireAuthorization()
  .RequireRateLimiting("AuthPolicy");

app.Run();