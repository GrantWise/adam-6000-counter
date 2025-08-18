using System.Text.Json.Serialization;

namespace Industrial.Adam.EquipmentScheduling.WebApi.Models;

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
    public Dictionary<string, string[]>? Errors { get; set; }

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
    /// <param name="errors">Additional error details</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Error API response</returns>
    public static ApiResponse<T> Failed(string errorMessage, Dictionary<string, string[]>? errors = null, string? correlationId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = errorMessage,
            Errors = errors,
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
    public Dictionary<string, string[]>? Errors { get; set; }

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
    /// Create a successful response without data
    /// </summary>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse Ok(string? correlationId = null)
    {
        return new ApiResponse
        {
            Success = true,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Create an error response without data
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errors">Additional error details</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>Error API response</returns>
    public static ApiResponse Failed(string errorMessage, Dictionary<string, string[]>? errors = null, string? correlationId = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = errorMessage,
            Errors = errors,
            CorrelationId = correlationId
        };
    }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
/// <typeparam name="T">The type of data being returned</typeparam>
public class PagedApiResponse<T> : ApiResponse<IEnumerable<T>>
{
    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of items
    /// </summary>
    public int TotalItems { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Indicates if there is a previous page
    /// </summary>
    [JsonIgnore]
    public bool HasPreviousPage => Page > 1;

    /// <summary>
    /// Indicates if there is a next page
    /// </summary>
    [JsonIgnore]
    public bool HasNextPage => Page < TotalPages;

    /// <summary>
    /// Creates a successful paginated response
    /// </summary>
    /// <param name="data">The response data</param>
    /// <param name="page">Current page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="totalItems">Total number of items</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>A successful paginated API response</returns>
    public static PagedApiResponse<T> Ok(IEnumerable<T> data, int page, int pageSize, int totalItems, string? correlationId = null)
    {
        return new PagedApiResponse<T>
        {
            Success = true,
            Data = data,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// Creates an error paginated response
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <param name="errors">Additional error details</param>
    /// <param name="correlationId">Optional correlation ID</param>
    /// <returns>An error paginated API response</returns>
    public static new PagedApiResponse<T> Failed(string errorMessage, Dictionary<string, string[]>? errors = null, string? correlationId = null)
    {
        return new PagedApiResponse<T>
        {
            Success = false,
            Error = errorMessage,
            Errors = errors,
            Page = 0,
            PageSize = 0,
            TotalItems = 0,
            TotalPages = 0,
            CorrelationId = correlationId
        };
    }
}
