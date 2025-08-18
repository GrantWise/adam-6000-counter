using System.Net;
using System.Text.Json;
using FluentValidation;

namespace Industrial.Adam.Oee.WebApi.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>
    /// Constructor for global exception middleware
    /// </summary>
    /// <param name="next">Next middleware in pipeline</param>
    /// <param name="logger">Logger instance</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handle exception and generate appropriate response
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="exception">Exception that occurred</param>
    /// <returns>Task</returns>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            ArgumentNullException nullEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid Request",
                Detail = $"Required parameter '{nullEx.ParamName}' is missing or null",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },

            ArgumentException argEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Invalid Request",
                Detail = argEx.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            },

            ValidationException validationEx => new ValidationProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "Validation Failed",
                Detail = "One or more validation errors occurred",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path,
                Errors = ParseValidationErrors(validationEx)
            },

            NotSupportedException notSupportedEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.5",
                Title = "Method Not Allowed",
                Detail = notSupportedEx.Message,
                Status = (int)HttpStatusCode.MethodNotAllowed,
                Instance = context.Request.Path
            },

            InvalidOperationException invalidOpEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Operation Not Allowed",
                Detail = invalidOpEx.Message,
                Status = (int)HttpStatusCode.NotFound,
                Instance = context.Request.Path
            },

            TimeoutException timeoutEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                Title = "Request Timeout",
                Detail = "The request took too long to process",
                Status = (int)HttpStatusCode.RequestTimeout,
                Instance = context.Request.Path
            },

            _ => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                Title = "Internal Server Error",
                Detail = "An unexpected error occurred while processing your request",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.Path
            }
        };

        // Add correlation ID if available
        if (context.TraceIdentifier != null)
        {
            problemDetails.Extensions["correlationId"] = context.TraceIdentifier;
        }

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var jsonResponse = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Parse FluentValidation errors into the format expected by ValidationProblemDetails
    /// </summary>
    /// <param name="validationException">Validation exception</param>
    /// <returns>Dictionary of field errors</returns>
    private static Dictionary<string, string[]> ParseValidationErrors(ValidationException validationException)
    {
        return validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ErrorMessage).ToArray()
            );
    }
}

/// <summary>
/// Standard Problem Details implementation
/// </summary>
public class ProblemDetails
{
    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The HTTP status code
    /// </summary>
    public int? Status { get; set; }

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    public string? Detail { get; set; }

    /// <summary>
    /// A URI reference that identifies the specific occurrence of the problem
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// Additional properties for the problem
    /// </summary>
    public Dictionary<string, object> Extensions { get; set; } = new();
}

/// <summary>
/// Problem Details for validation errors
/// </summary>
public class ValidationProblemDetails : ProblemDetails
{
    /// <summary>
    /// Validation errors grouped by field name
    /// </summary>
    public Dictionary<string, string[]> Errors { get; set; } = new();
}
