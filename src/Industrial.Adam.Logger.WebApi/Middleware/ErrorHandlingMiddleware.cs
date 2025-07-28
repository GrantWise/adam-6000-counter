using System.Net;
using System.Text.Json;
using Industrial.Adam.Logger.Interfaces;

namespace Industrial.Adam.Logger.WebApi.Middleware;

/// <summary>
/// Global error handling middleware for consistent API error responses
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIndustrialErrorService? errorService = null)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, errorService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IIndustrialErrorService? errorService)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse();

        switch (exception)
        {
            case KeyNotFoundException or FileNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Title = "Resource Not Found";
                errorResponse.Detail = exception.Message;
                errorResponse.Status = (int)HttpStatusCode.NotFound;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Title = "Unauthorized";
                errorResponse.Detail = "You are not authorized to access this resource";
                errorResponse.Status = (int)HttpStatusCode.Unauthorized;
                break;

            case ArgumentException or InvalidOperationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Title = "Bad Request";
                errorResponse.Detail = exception.Message;
                errorResponse.Status = (int)HttpStatusCode.BadRequest;
                break;

            case TimeoutException:
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Title = "Request Timeout";
                errorResponse.Detail = "The operation timed out";
                errorResponse.Status = (int)HttpStatusCode.RequestTimeout;
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Title = "Internal Server Error";
                errorResponse.Detail = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment()
                    ? exception.ToString()
                    : "An error occurred while processing your request";
                errorResponse.Status = (int)HttpStatusCode.InternalServerError;
                break;
        }

        // Add industrial error context if available
        if (errorService != null)
        {
            var industrialError = errorService.CreateAndLogError(
                exception,
                $"API-{response.StatusCode}",
                errorResponse.Title,
                new Dictionary<string, object>
                {
                    ["RequestPath"] = context.Request.Path,
                    ["RequestMethod"] = context.Request.Method
                }
            );

            errorResponse.ErrorCode = industrialError.ErrorCode;
            errorResponse.TroubleshootingSteps = industrialError.TroubleshootingSteps.ToList();
            errorResponse.CorrelationId = context.TraceIdentifier;
        }

        errorResponse.Instance = context.Request.Path;
        errorResponse.Timestamp = DateTimeOffset.UtcNow;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(errorResponse, jsonOptions);
        await response.WriteAsync(json);
    }
}

/// <summary>
/// Standard error response format based on RFC 7807 Problem Details
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    public string Detail { get; set; } = string.Empty;

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    public string Instance { get; set; } = string.Empty;

    /// <summary>
    /// Application-specific error code
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Troubleshooting steps for industrial context
    /// </summary>
    public List<string>? TroubleshootingSteps { get; set; }

    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp of the error
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}