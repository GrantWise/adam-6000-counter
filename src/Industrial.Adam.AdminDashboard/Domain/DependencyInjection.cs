using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.AdminDashboard.Domain;

/// <summary>
/// Domain layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add AdminDashboard Domain services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAdminDashboardDomain(this IServiceCollection services)
    {
        // Domain services would be registered here when implemented
        // Currently no concrete domain services to register
        
        return services;
    }
}