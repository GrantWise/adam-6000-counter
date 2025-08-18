namespace Industrial.Adam.Oee.Domain.Enums;

/// <summary>
/// Error codes specific to OEE domain operations
/// Provides categorized error identification for monitoring and alerting
/// </summary>
public enum OeeErrorCode
{
    /// <summary>
    /// Unknown or unclassified error
    /// </summary>
    Unknown = 0,

    // Calculation Errors (1000-1999)
    /// <summary>
    /// General OEE calculation failure
    /// </summary>
    CalculationFailed = 1000,

    /// <summary>
    /// Insufficient data for reliable calculation
    /// </summary>
    InsufficientData = 1001,

    /// <summary>
    /// Invalid calculation period
    /// </summary>
    InvalidPeriod = 1002,

    /// <summary>
    /// Availability calculation error
    /// </summary>
    AvailabilityCalculationFailed = 1003,

    /// <summary>
    /// Performance calculation error
    /// </summary>
    PerformanceCalculationFailed = 1004,

    /// <summary>
    /// Quality calculation error
    /// </summary>
    QualityCalculationFailed = 1005,

    // Data Access Errors (2000-2999)
    /// <summary>
    /// Counter data not available
    /// </summary>
    DataNotAvailable = 2000,

    /// <summary>
    /// Database connection failure
    /// </summary>
    DatabaseConnectionFailed = 2001,

    /// <summary>
    /// Database query timeout
    /// </summary>
    DatabaseTimeout = 2002,

    /// <summary>
    /// Data consistency error
    /// </summary>
    DataConsistencyError = 2003,

    // Work Order Errors (3000-3999)
    /// <summary>
    /// Work order not found
    /// </summary>
    WorkOrderNotFound = 3000,

    /// <summary>
    /// Invalid work order state
    /// </summary>
    InvalidWorkOrderState = 3001,

    /// <summary>
    /// Work order validation failed
    /// </summary>
    WorkOrderValidationFailed = 3002,

    /// <summary>
    /// Work order operation not allowed
    /// </summary>
    WorkOrderOperationNotAllowed = 3003,

    // Device/Resource Errors (4000-4999)
    /// <summary>
    /// Device not responding
    /// </summary>
    DeviceNotResponding = 4000,

    /// <summary>
    /// Invalid device configuration
    /// </summary>
    InvalidDeviceConfiguration = 4001,

    /// <summary>
    /// Device communication error
    /// </summary>
    DeviceCommunicationError = 4002,

    // Configuration Errors (5000-5999)
    /// <summary>
    /// Invalid OEE configuration
    /// </summary>
    InvalidConfiguration = 5000,

    /// <summary>
    /// Missing required configuration
    /// </summary>
    MissingConfiguration = 5001,

    /// <summary>
    /// Configuration validation failed
    /// </summary>
    ConfigurationValidationFailed = 5002
}

/// <summary>
/// Extension methods for OeeErrorCode
/// </summary>
public static class OeeErrorCodeExtensions
{
    /// <summary>
    /// Get the category of the error code
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Error category</returns>
    public static string GetCategory(this OeeErrorCode errorCode)
    {
        return ((int)errorCode) switch
        {
            >= 1000 and < 2000 => "Calculation",
            >= 2000 and < 3000 => "DataAccess",
            >= 3000 and < 4000 => "WorkOrder",
            >= 4000 and < 5000 => "Device",
            >= 5000 and < 6000 => "Configuration",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Check if the error is retryable
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>True if the error is retryable</returns>
    public static bool IsRetryable(this OeeErrorCode errorCode)
    {
        return errorCode switch
        {
            OeeErrorCode.DatabaseConnectionFailed => true,
            OeeErrorCode.DatabaseTimeout => true,
            OeeErrorCode.DeviceNotResponding => true,
            OeeErrorCode.DeviceCommunicationError => true,
            _ => false
        };
    }

    /// <summary>
    /// Get the severity level of the error
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <returns>Error severity</returns>
    public static ErrorSeverity GetSeverity(this OeeErrorCode errorCode)
    {
        return errorCode switch
        {
            OeeErrorCode.DatabaseConnectionFailed => ErrorSeverity.Critical,
            OeeErrorCode.WorkOrderNotFound => ErrorSeverity.High,
            OeeErrorCode.InvalidWorkOrderState => ErrorSeverity.High,
            OeeErrorCode.DataNotAvailable => ErrorSeverity.Medium,
            OeeErrorCode.InsufficientData => ErrorSeverity.Medium,
            OeeErrorCode.InvalidPeriod => ErrorSeverity.Low,
            OeeErrorCode.DeviceNotResponding => ErrorSeverity.High,
            _ => ErrorSeverity.Medium
        };
    }
}

/// <summary>
/// Error severity levels
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// Low severity - informational
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium severity - warning
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High severity - error
    /// </summary>
    High = 3,

    /// <summary>
    /// Critical severity - system failure
    /// </summary>
    Critical = 4
}
