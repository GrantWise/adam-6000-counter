namespace Industrial.Adam.Oee.Infrastructure.Models;

/// <summary>
/// Simple API response wrapper for Equipment Scheduling integration
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
    /// Create a successful response
    /// </summary>
    /// <param name="data">The response data</param>
    /// <returns>Successful API response</returns>
    public static ApiResponse<T> Ok(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    /// <param name="errorMessage">Error message</param>
    /// <returns>Error API response</returns>
    public static ApiResponse<T> Failed(string errorMessage)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = errorMessage
        };
    }
}