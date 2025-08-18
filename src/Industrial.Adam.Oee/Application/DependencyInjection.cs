using System.Reflection;
using FluentValidation;
using Industrial.Adam.Oee.Application.Events;
using Industrial.Adam.Oee.Application.Interfaces;
using Industrial.Adam.Oee.Application.Services;
using Industrial.Adam.Oee.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Application;

/// <summary>
/// Application layer dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add OEE Application services to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddOeeApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // Add custom behaviors
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Add application services
        services.AddScoped<IOeeApplicationService, OeeApplicationService>();

        // Add event handlers
        services.AddScoped<IEventHandler<StoppageDetectedEvent>, StoppageDetectedEventHandler>();

        // Add background services
        services.AddHostedService<StoppageMonitoringBackgroundService>();

        // Add memory cache for application-level caching
        services.AddMemoryCache();

        return services;
    }
}

/// <summary>
/// Validation behavior for MediatR pipeline
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    /// <summary>
    /// Constructor for validation behavior
    /// </summary>
    /// <param name="validators">Collection of validators for the request</param>
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    /// <summary>
    /// Handle the request with validation
    /// </summary>
    /// <param name="request">Request to validate and handle</param>
    /// <param name="next">Next delegate in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the handler</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}

/// <summary>
/// Logging behavior for MediatR pipeline
/// </summary>
/// <typeparam name="TRequest">Request type</typeparam>
/// <typeparam name="TResponse">Response type</typeparam>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    /// <summary>
    /// Constructor for logging behavior
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle the request with logging
    /// </summary>
    /// <param name="request">Request to log and handle</param>
    /// <param name="next">Next delegate in the pipeline</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Response from the handler</returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling request: {RequestName}", requestName);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var response = await next();

            stopwatch.Stop();
            _logger.LogInformation("Request {RequestName} completed in {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMs}ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
