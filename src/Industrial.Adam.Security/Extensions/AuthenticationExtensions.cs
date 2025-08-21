using System.Text;
using Industrial.Adam.Security.Authentication;
using Industrial.Adam.Security.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Industrial.Adam.Security.Extensions;

/// <summary>
/// Extensions for adding JWT authentication and authorization
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds JWT authentication services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Add authentication services
        services.AddScoped<JwtAuthenticationService>();
        services.AddScoped<UserStorageService>();

        // Configure JWT authentication
        var secretKey = configuration["JWT_SECRET_KEY"] ?? throw new InvalidOperationException("JWT_SECRET_KEY not configured");
        var issuer = configuration["JWT_ISSUER"] ?? "Industrial.Adam.System";
        var audience = configuration["JWT_AUDIENCE"] ?? "Industrial.Adam.APIs";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception != null)
                    {
                        context.Response.Headers.TryAdd("Token-Error", context.Exception.Message);
                    }
                    return Task.CompletedTask;
                },
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";

                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = "Unauthorized",
                        message = "You must be authenticated to access this resource"
                    });

                    await context.Response.WriteAsync(result);
                }
            };
        });

        // Add authorization with role-based policies
        services.AddAuthorization(options =>
        {
            // Require authenticated user by default
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Role-based policies
            options.AddPolicy("RequireSystemAdmin", policy =>
                policy.RequireRole(RoleConstants.SystemAdmin));

            options.AddPolicy("RequireAdmin", policy =>
                policy.RequireRole(RoleConstants.AdminRoles));

            options.AddPolicy("RequireProduction", policy =>
                policy.RequireRole(RoleConstants.ProductionRoles));

            options.AddPolicy("RequireOperational", policy =>
                policy.RequireRole(RoleConstants.OperationalRoles));

            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireRole(RoleConstants.AuthenticatedRoles));
        });

        return services;
    }

    /// <summary>
    /// Adds CORS policy with environment-based configuration
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddSecureCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("SecurePolicy", policy =>
            {
                var origins = configuration.GetCorsOrigins();
                var methods = configuration.GetCorsMethods();
                var headers = configuration.GetCorsHeaders();

                if (origins.Length > 0)
                {
                    policy.WithOrigins(origins);
                }
                else
                {
                    // Fallback for development - still better than AllowAnyOrigin
                    policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "https://localhost:5001");
                }

                policy.WithMethods(methods)
                      .WithHeaders(headers)
                      .AllowCredentials();
            });

            // Add a development policy for local testing
            options.AddPolicy("Development", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "https://localhost:5001")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds complete Industrial Adam security package
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddIndustrialAdamSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddEnvironmentValidation()
            .AddJwtAuthentication(configuration)
            .AddSecureCors(configuration);
    }
}
