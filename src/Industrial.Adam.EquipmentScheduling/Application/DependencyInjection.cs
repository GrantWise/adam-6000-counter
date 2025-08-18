using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Industrial.Adam.EquipmentScheduling.Application;

/// <summary>
/// Extension methods for registering application services
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for method chaining</returns>
    public static IServiceCollection AddEquipmentSchedulingApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Register application services
        // Add any application-specific services here

        return services;
    }
}
