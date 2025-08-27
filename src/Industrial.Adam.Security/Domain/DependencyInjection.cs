using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.Security.Domain;

/// <summary>
/// Domain layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds domain services to the service collection
    /// </summary>
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        // Domain services are registered as interfaces and implemented in Infrastructure layer
        // This follows Clean Architecture principles where Domain defines contracts
        // but doesn't have implementations (no dependencies on external concerns)
        
        return services;
    }
}