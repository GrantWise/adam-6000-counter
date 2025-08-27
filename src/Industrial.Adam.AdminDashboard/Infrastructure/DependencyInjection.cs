using Industrial.Adam.AdminDashboard.Domain.Interfaces;
using Industrial.Adam.AdminDashboard.Infrastructure.Repositories;
using Industrial.Adam.AdminDashboard.Infrastructure.Services;
using Industrial.Adam.AdminDashboard.Infrastructure.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.AdminDashboard.Infrastructure;

/// <summary>
/// Infrastructure layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add AdminDashboard Infrastructure services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration instance</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAdminDashboardInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register repositories
        services.AddScoped<ISystemHealthMetricRepository, SystemHealthMetricRepository>();
        services.AddScoped<ISystemAlertRepository, SystemAlertRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        // Register domain services
        services.AddScoped<ISystemHealthService, SystemHealthService>();

        // Register SignalR services
        services.AddScoped<IHealthUpdateService, HealthUpdateService>();

        // Add background services for health monitoring
        services.AddHostedService<HealthMonitoringBackgroundService>();

        return services;
    }
}

// Note: We'll need to create the missing repositories and background service
// For now, creating placeholder implementations to avoid compilation errors