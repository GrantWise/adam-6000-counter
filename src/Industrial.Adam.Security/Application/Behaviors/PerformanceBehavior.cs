using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Industrial.Adam.Security.Application.Behaviors;

/// <summary>
/// MediatR behavior for performance monitoring and alerting
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int PerformanceThresholdMs = 1000; // 1 second threshold

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();
        var elapsedMs = stopwatch.ElapsedMilliseconds;

        if (elapsedMs > PerformanceThresholdMs)
        {
            _logger.LogWarning("Long running request detected: {RequestName} took {ElapsedMs}ms to complete", 
                requestName, elapsedMs);
        }
        else
        {
            _logger.LogDebug("Request {RequestName} completed in {ElapsedMs}ms", requestName, elapsedMs);
        }

        return response;
    }
}