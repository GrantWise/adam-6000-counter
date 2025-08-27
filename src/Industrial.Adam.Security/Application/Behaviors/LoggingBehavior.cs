using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Industrial.Adam.Security.Application.Behaviors;

/// <summary>
/// MediatR behavior for logging request/response information
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var requestId = Guid.NewGuid().ToString()[..8];

        _logger.LogInformation("Processing request {RequestName} with ID {RequestId}", requestName, requestId);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var response = await next();
            
            stopwatch.Stop();
            _logger.LogInformation("Completed request {RequestName} with ID {RequestId} in {ElapsedMs}ms", 
                requestName, requestId, stopwatch.ElapsedMilliseconds);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} with ID {RequestId} failed after {ElapsedMs}ms", 
                requestName, requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}