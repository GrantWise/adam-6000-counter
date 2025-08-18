using Industrial.Adam.EquipmentScheduling.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.EquipmentScheduling.Domain;

/// <summary>
/// Extension methods for registering domain services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers domain services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEquipmentSchedulingDomain(this IServiceCollection services)
    {
        // Register domain services
        services.AddScoped<ScheduleGenerationService>();

        return services;
    }
}
