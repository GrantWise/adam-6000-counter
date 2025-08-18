namespace Industrial.Adam.Oee.WebApi.Models;

/// <summary>
/// Standard API response wrapper for consistent response format
/// </summary>
/// <typeparam name="T">Type of the data payload</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The response data payload
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Create a successful response
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse<T> Ok(T data, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Error API response</returns>
    public static ApiResponse<T> Failed(string errorMessage, object? errorDetails = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = errorMessage,
            ErrorDetails = errorDetails,
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// Non-generic API response for responses without data payload
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Additional error details for debugging
    /// </summary>
    public object? ErrorDetails { get; set; }

    /// <summary>
    /// Timestamp when the response was generated
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Request correlation ID for tracing
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Create a successful response with data
    /// </summary>
    /// <typeparam name="T">Type of the data</typeparam>
    /// <param name="data">The response data</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse<T> WithData<T>(T data, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Create an error response without data
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errorDetails">Additional error details</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Error API response</returns>
    public static ApiResponse Failed(string errorMessage, object? errorDetails = null, string? correlationId = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = errorMessage,
            ErrorDetails = errorDetails,
            CorrelationId = correlationId
        };
    }
}
