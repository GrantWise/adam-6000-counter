using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Domain.Services;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Configuration;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <param name="environment">The host environment</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEquipmentSchedulingInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configuration
        services.Configure<EquipmentSchedulingSettings>(
            configuration.GetSection(EquipmentSchedulingSettings.SectionName));

        // Database Context
        var connectionString = configuration.GetConnectionString("EquipmentScheduling")
            ?? configuration.GetSection($"{EquipmentSchedulingSettings.SectionName}:ConnectionString").Value
            ?? throw new InvalidOperationException("Equipment Scheduling connection string is not configured");

        services.AddDbContext<EquipmentSchedulingDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(EquipmentSchedulingDbContext).Assembly.FullName);
                npgsqlOptions.CommandTimeout(30);
            });

            // Enable sensitive data logging in development
            if (environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }

            // Use default logging - will be configured at application level
        });

        // Repositories
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IOperatingPatternRepository, OperatingPatternRepository>();
        services.AddScoped<IPatternAssignmentRepository, PatternAssignmentRepository>();
        services.AddScoped<IEquipmentScheduleRepository, EquipmentScheduleRepository>();

        // Domain Services
        services.AddScoped<ScheduleGenerationService>();

        // Health Checks
        services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "equipment-scheduling-database",
                tags: ["database", "equipment-scheduling"]);

        // Memory Cache
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and migrated
    /// </summary>
    /// <param name="serviceProvider">The service provider</param>
    /// <returns>A task representing the async operation</returns>
    public static async Task EnsureDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EquipmentSchedulingDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<EquipmentSchedulingDbContext>>();

        try
        {
            logger.LogInformation("Ensuring Equipment Scheduling database exists");
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Equipment Scheduling database is ready");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure Equipment Scheduling database");
            throw;
        }
    }
}
