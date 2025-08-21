using System.Text.Json.Serialization;

namespace Industrial.Adam.Oee.Tests.TestDoubles;

/// <summary>
/// Test double for Equipment Scheduling API response wrapper
/// Used only in integration tests to simulate Equipment Scheduling API responses
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
}
