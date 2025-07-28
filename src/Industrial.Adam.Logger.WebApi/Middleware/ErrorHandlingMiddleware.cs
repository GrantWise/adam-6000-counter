using System.Net;
using System.Text.Json;

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

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
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

        errorResponse.Instance = context.Request.Path;
        errorResponse.Timestamp = DateTimeOffset.UtcNow;
        errorResponse.CorrelationId = context.TraceIdentifier;

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
    /// Correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Timestamp of the error
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}