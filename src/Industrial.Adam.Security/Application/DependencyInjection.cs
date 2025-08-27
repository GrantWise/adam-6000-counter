using FluentValidation;
using Industrial.Adam.Security.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Industrial.Adam.Security.Application;

/// <summary>
/// Application layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds application services to the service collection
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR for CQRS pattern
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Add FluentValidation validators
        services.AddValidatorsFromAssembly(assembly);

        // Add application behaviors for cross-cutting concerns
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

        return services;
    }
}