using Industrial.Adam.Oee.Domain.Enums;

namespace Industrial.Adam.Oee.Domain.Exceptions;

/// <summary>
/// Base exception for all OEE domain-specific errors
/// Provides structured error information for monitoring and alerting
/// </summary>
public abstract class OeeException : Exception
{
    /// <summary>
    /// OEE-specific error code
    /// </summary>
    public OeeErrorCode ErrorCode { get; }

    /// <summary>
    /// Error severity level
    /// </summary>
    public ErrorSeverity Severity { get; }

    /// <summary>
    /// Device ID related to the error (if applicable)
    /// </summary>
    public string? DeviceId { get; }

    /// <summary>
    /// Work order ID related to the error (if applicable)
    /// </summary>
    public string? WorkOrderId { get; }

    /// <summary>
    /// Additional context data
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// When the error occurred
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Base constructor for OEE exceptions
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="errorCode">OEE error code</param>
    /// <param name="deviceId">Device ID (optional)</param>
    /// <param name="workOrderId">Work order ID (optional)</param>
    /// <param name="innerException">Inner exception (optional)</param>
    protected OeeException(
        string message,
        OeeErrorCode errorCode,
        string? deviceId = null,
        string? workOrderId = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        Severity = errorCode.GetSeverity();
        DeviceId = deviceId;
        WorkOrderId = workOrderId;
        Context = new Dictionary<string, object>();
        Timestamp = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Add context information to the exception
    /// </summary>
    /// <param name="key">Context key</param>
    /// <param name="value">Context value</param>
    /// <returns>This exception for fluent chaining</returns>
    public OeeException WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    /// <summary>
    /// Get formatted error details for logging
    /// </summary>
    /// <returns>Formatted error details</returns>
    public virtual string GetErrorDetails()
    {
        var details = new List<string>
        {
            $"ErrorCode: {ErrorCode} ({ErrorCode.GetCategory()})",
            $"Severity: {Severity}",
            $"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}"
        };

        if (!string.IsNullOrEmpty(DeviceId))
            details.Add($"DeviceId: {DeviceId}");

        if (!string.IsNullOrEmpty(WorkOrderId))
            details.Add($"WorkOrderId: {WorkOrderId}");

        if (Context.Any())
        {
            details.Add("Context:");
            foreach (var kvp in Context)
            {
                details.Add($"  {kvp.Key}: {kvp.Value}");
            }
        }

        return string.Join(Environment.NewLine, details);
    }
}

/// <summary>
/// Exception for OEE calculation errors
/// </summary>
public sealed class OeeCalculationException : OeeException
{
    /// <summary>
    /// Calculation type that failed
    /// </summary>
    public string CalculationType { get; }

    /// <summary>
    /// Period start time
    /// </summary>
    public DateTime? StartTime { get; }

    /// <summary>
    /// Period end time
    /// </summary>
    public DateTime? EndTime { get; }

    /// <summary>
    /// Create a new OEE calculation exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="calculationType">Type of calculation that failed</param>
    /// <param name="errorCode">Specific error code</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="startTime">Period start time</param>
    /// <param name="endTime">Period end time</param>
    /// <param name="innerException">Inner exception</param>
    public OeeCalculationException(
        string message,
        string calculationType,
        OeeErrorCode errorCode = OeeErrorCode.CalculationFailed,
        string? deviceId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        Exception? innerException = null)
        : base(message, errorCode, deviceId, null, innerException)
    {
        CalculationType = calculationType;
        StartTime = startTime;
        EndTime = endTime;

        WithContext("CalculationType", calculationType);
        if (startTime.HasValue)
            WithContext("StartTime", startTime.Value);
        if (endTime.HasValue)
            WithContext("EndTime", endTime.Value);
    }
}

/// <summary>
/// Exception for work order related errors
/// </summary>
public sealed class WorkOrderException : OeeException
{
    /// <summary>
    /// Work order status when error occurred
    /// </summary>
    public string? WorkOrderStatus { get; }

    /// <summary>
    /// Operation that was attempted
    /// </summary>
    public string? Operation { get; }

    /// <summary>
    /// Create a new work order exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="workOrderId">Work order ID</param>
    /// <param name="errorCode">Specific error code</param>
    /// <param name="workOrderStatus">Work order status</param>
    /// <param name="operation">Operation attempted</param>
    /// <param name="innerException">Inner exception</param>
    public WorkOrderException(
        string message,
        string workOrderId,
        OeeErrorCode errorCode = OeeErrorCode.WorkOrderValidationFailed,
        string? workOrderStatus = null,
        string? operation = null,
        Exception? innerException = null)
        : base(message, errorCode, null, workOrderId, innerException)
    {
        WorkOrderStatus = workOrderStatus;
        Operation = operation;

        if (!string.IsNullOrEmpty(workOrderStatus))
            WithContext("WorkOrderStatus", workOrderStatus);
        if (!string.IsNullOrEmpty(operation))
            WithContext("Operation", operation);
    }
}

/// <summary>
/// Exception for data access errors
/// </summary>
public sealed class OeeDataException : OeeException
{
    /// <summary>
    /// Data source that failed
    /// </summary>
    public string DataSource { get; }

    /// <summary>
    /// Query or operation that failed
    /// </summary>
    public string? Query { get; }

    /// <summary>
    /// Create a new data access exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="dataSource">Data source name</param>
    /// <param name="errorCode">Specific error code</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="query">Query or operation</param>
    /// <param name="innerException">Inner exception</param>
    public OeeDataException(
        string message,
        string dataSource,
        OeeErrorCode errorCode = OeeErrorCode.DataNotAvailable,
        string? deviceId = null,
        string? query = null,
        Exception? innerException = null)
        : base(message, errorCode, deviceId, null, innerException)
    {
        DataSource = dataSource;
        Query = query;

        WithContext("DataSource", dataSource);
        if (!string.IsNullOrEmpty(query))
            WithContext("Query", query);
    }
}

/// <summary>
/// Exception for device communication errors
/// </summary>
public sealed class DeviceException : OeeException
{
    /// <summary>
    /// Device connection state
    /// </summary>
    public string? ConnectionState { get; }

    /// <summary>
    /// Last successful communication time
    /// </summary>
    public DateTimeOffset? LastCommunication { get; }

    /// <summary>
    /// Create a new device exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="deviceId">Device ID</param>
    /// <param name="errorCode">Specific error code</param>
    /// <param name="connectionState">Connection state</param>
    /// <param name="lastCommunication">Last communication time</param>
    /// <param name="innerException">Inner exception</param>
    public DeviceException(
        string message,
        string deviceId,
        OeeErrorCode errorCode = OeeErrorCode.DeviceNotResponding,
        string? connectionState = null,
        DateTimeOffset? lastCommunication = null,
        Exception? innerException = null)
        : base(message, errorCode, deviceId, null, innerException)
    {
        ConnectionState = connectionState;
        LastCommunication = lastCommunication;

        if (!string.IsNullOrEmpty(connectionState))
            WithContext("ConnectionState", connectionState);
        if (lastCommunication.HasValue)
            WithContext("LastCommunication", lastCommunication.Value);
    }
}

/// <summary>
/// Exception for configuration errors
/// </summary>
public sealed class OeeConfigurationException : OeeException
{
    /// <summary>
    /// Configuration section that failed
    /// </summary>
    public string ConfigurationSection { get; }

    /// <summary>
    /// Configuration key that caused the error
    /// </summary>
    public string? ConfigurationKey { get; }

    /// <summary>
    /// Create a new configuration exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="configurationSection">Configuration section</param>
    /// <param name="errorCode">Specific error code</param>
    /// <param name="configurationKey">Configuration key</param>
    /// <param name="innerException">Inner exception</param>
    public OeeConfigurationException(
        string message,
        string configurationSection,
        OeeErrorCode errorCode = OeeErrorCode.InvalidConfiguration,
        string? configurationKey = null,
        Exception? innerException = null)
        : base(message, errorCode, null, null, innerException)
    {
        ConfigurationSection = configurationSection;
        ConfigurationKey = configurationKey;

        WithContext("ConfigurationSection", configurationSection);
        if (!string.IsNullOrEmpty(configurationKey))
            WithContext("ConfigurationKey", configurationKey);
    }
}
