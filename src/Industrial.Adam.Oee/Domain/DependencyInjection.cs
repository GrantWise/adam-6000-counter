using Industrial.Adam.Oee.Domain.Interfaces;
using Industrial.Adam.Oee.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Industrial.Adam.Oee.Domain;

/// <summary>
/// Dependency injection configuration for OEE Domain layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add OEE domain services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOeeDomain(this IServiceCollection services)
    {
        // Calculation Services
        services.TryAddScoped<IAvailabilityCalculationService, AvailabilityCalculationService>();
        services.TryAddScoped<IPerformanceCalculationService, PerformanceCalculationService>();
        services.TryAddScoped<IQualityCalculationService, QualityCalculationService>();
        services.TryAddScoped<IOeeCalculationService, OeeCalculationService>();

        // Work Order Services
        services.TryAddScoped<IWorkOrderProgressService, WorkOrderProgressService>();
        services.TryAddScoped<IWorkOrderValidationService, WorkOrderValidationService>();

        // Job Sequencing Services  
        services.TryAddScoped<IJobSequencingService, JobSequencingService>();

        // Equipment Services
        services.TryAddScoped<IEquipmentLineService, EquipmentLineService>();

        // Error Handling Services
        services.TryAddSingleton<IIndustrialOeeErrorService, IndustrialOeeErrorService>();

        // Stoppage Detection Services
        services.TryAddScoped<IStoppageDetectionService, StoppageDetectionService>();

        return services;
    }
}
