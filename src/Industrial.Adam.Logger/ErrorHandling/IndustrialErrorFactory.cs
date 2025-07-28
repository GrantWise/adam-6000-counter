using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Industrial.Adam.Logger.Models;

namespace Industrial.Adam.Logger.ErrorHandling;

/// <summary>
/// Factory for creating standardized industrial error messages with troubleshooting guidance
/// Provides consistent error messaging across the entire application
/// </summary>
public static class IndustrialErrorFactory
{
    #region Connection Errors

    /// <summary>
    /// Create error message for device connection failures
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Device port</param>
    /// <param name="exception">Original exception</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateConnectionFailure(
        string deviceId,
        string ipAddress,
        int port,
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "CONN-001",
            Summary = $"Failed to establish connection to device '{deviceId}' at {ipAddress}:{port}",
            DetailedDescription = $"Unable to establish TCP connection to device. Network error: {exception.Message}",
            TroubleshootingSteps = new List<string>
            {
                $"1. VERIFY NETWORK: Ping device at {ipAddress} from the server",
                $"2. CHECK PORT: Verify Modbus TCP port {port} is open and accessible",
                $"3. DEVICE STATUS: Check if device power LED is solid green",
                $"4. NETWORK SETTINGS: Verify device IP configuration matches {ipAddress}",
                $"5. FIREWALL: Check for firewall blocking port {port}",
                $"6. CABLE CONNECTION: Verify Ethernet cable is properly connected",
                $"7. NETWORK SWITCH: Check switch port status and activity LEDs"
            },
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["IpAddress"] = ipAddress,
                ["Port"] = port,
                ["ExceptionType"] = exception.GetType().Name,
                ["NetworkInterface"] = Environment.MachineName,
                ["AttemptTimestamp"] = DateTime.UtcNow
            },
            EscalationProcedure = "If all troubleshooting steps fail, contact network administrator. Provide device MAC address and network configuration details.",
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.Connection,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            DeviceId = deviceId,
            OriginalException = exception,
            TechnicalDetails = $"TCP connection attempt to {ipAddress}:{port} failed with {exception.GetType().Name}: {exception.Message}"
        };
    }

    /// <summary>
    /// Create error message for connection timeout
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="ipAddress">Device IP address</param>
    /// <param name="port">Device port</param>
    /// <param name="timeoutMs">Timeout duration in milliseconds</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateConnectionTimeout(
        string deviceId,
        string ipAddress,
        int port,
        int timeoutMs,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "CONN-002",
            Summary = $"Connection timeout to device '{deviceId}' after {timeoutMs}ms",
            DetailedDescription = $"Device at {ipAddress}:{port} did not respond within {timeoutMs}ms timeout period",
            TroubleshootingSteps = new List<string>
            {
                $"1. RESPONSE TIME: Check if device is responding slowly due to high load",
                $"2. NETWORK LATENCY: Test network latency with ping -t {ipAddress}",
                $"3. INCREASE TIMEOUT: Consider increasing timeout to {timeoutMs * 2}ms",
                $"4. DEVICE LOAD: Check if device is processing too many concurrent requests",
                $"5. NETWORK CONGESTION: Verify network is not congested",
                $"6. DEVICE HEALTH: Check device CPU and memory usage if accessible",
                $"7. RESTART DEVICE: Power cycle the device if other steps fail"
            },
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["IpAddress"] = ipAddress,
                ["Port"] = port,
                ["TimeoutMs"] = timeoutMs,
                ["SuggestedTimeoutMs"] = timeoutMs * 2,
                ["NetworkInterface"] = Environment.MachineName
            },
            EscalationProcedure = "If timeouts persist, contact device manufacturer support. Provide device model, firmware version, and network configuration.",
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Connection,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            DeviceId = deviceId,
            TechnicalDetails = $"TCP connection to {ipAddress}:{port} timed out after {timeoutMs}ms"
        };
    }

    #endregion

    #region Communication Errors

    /// <summary>
    /// Create error message for Modbus communication failures
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="operation">Modbus operation (e.g., "Read Holding Registers")</param>
    /// <param name="startAddress">Starting register address</param>
    /// <param name="count">Number of registers</param>
    /// <param name="attempt">Current attempt number</param>
    /// <param name="maxAttempts">Maximum number of attempts</param>
    /// <param name="exception">Original exception</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateModbusCommunicationFailure(
        string deviceId,
        string operation,
        ushort startAddress,
        ushort count,
        int attempt,
        int maxAttempts,
        Exception exception,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "COMM-002",
            Summary = $"Modbus {operation} failed for device '{deviceId}' (attempt {attempt}/{maxAttempts})",
            DetailedDescription = $"Failed to {operation.ToLower()} {count} registers starting at address {startAddress}. Error: {exception.Message}",
            TroubleshootingSteps = new List<string>
            {
                $"1. REGISTER RANGE: Verify register addresses {startAddress}-{startAddress + count - 1} are valid for this device",
                $"2. UNIT ID: Check Modbus unit ID configuration matches device DIP switches",
                $"3. DEVICE MODE: Ensure device is in normal operation mode, not configuration mode",
                $"4. REGISTER ACCESS: Verify device supports {operation} at specified addresses",
                $"5. CONCURRENT ACCESS: Verify no other Modbus clients are accessing the device",
                $"6. BAUD RATE: Check communication settings match device configuration",
                $"7. DEVICE MANUAL: Consult device manual for correct register map"
            },
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["Operation"] = operation,
                ["StartAddress"] = startAddress,
                ["RegisterCount"] = count,
                ["AttemptNumber"] = attempt,
                ["MaxAttempts"] = maxAttempts,
                ["ModbusFunction"] = GetModbusFunctionCode(operation),
                ["AddressRange"] = $"{startAddress}-{startAddress + count - 1}"
            },
            EscalationProcedure = "Contact device manufacturer support if register addresses are correct. Provide device model, firmware version, and Modbus configuration.",
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Communication,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            DeviceId = deviceId,
            OriginalException = exception,
            TechnicalDetails = $"Modbus {operation} failed: StartAddress={startAddress}, Count={count}, Exception={exception.GetType().Name}"
        };
    }

    #endregion

    #region Data Validation Errors

    /// <summary>
    /// Create error message for data validation failures
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="value">Invalid value</param>
    /// <param name="validationRule">Validation rule that failed</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateDataValidationFailure(
        string deviceId,
        int channel,
        object value,
        string validationRule,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "DATA-003",
            Summary = $"Data validation failed for device '{deviceId}' channel {channel}",
            DetailedDescription = $"Received value '{value}' failed validation rule: {validationRule}",
            TroubleshootingSteps = new List<string>
            {
                $"1. SENSOR CHECK: Verify sensor connected to channel {channel} is functioning correctly",
                $"2. WIRING: Check sensor wiring for loose connections or damage",
                $"3. CALIBRATION: Verify sensor calibration is within acceptable range",
                $"4. VALIDATION RULES: Review validation rules for channel {channel} configuration",
                $"5. ENVIRONMENTAL: Check for environmental factors affecting sensor readings",
                $"6. COMPARISON: Compare with historical data patterns for this channel",
                $"7. MANUAL VERIFICATION: Manually verify the physical process to confirm reading accuracy"
            },
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["Channel"] = channel,
                ["ReceivedValue"] = value,
                ["ValidationRule"] = validationRule,
                ["ValueType"] = value.GetType().Name,
                ["Timestamp"] = DateTime.UtcNow
            },
            EscalationProcedure = "If sensor and wiring are correct, contact process engineer to review validation rules and expected ranges.",
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Data,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            DeviceId = deviceId,
            Channel = channel,
            TechnicalDetails = $"Data validation failed: Value={value}, Rule={validationRule}, Type={value.GetType().Name}"
        };
    }

    /// <summary>
    /// Create error message for counter overflow detection
    /// </summary>
    /// <param name="deviceId">Device identifier</param>
    /// <param name="channel">Channel number</param>
    /// <param name="currentValue">Current counter value</param>
    /// <param name="previousValue">Previous counter value</param>
    /// <param name="maxValue">Maximum counter value</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateCounterOverflowDetection(
        string deviceId,
        int channel,
        long currentValue,
        long previousValue,
        long maxValue,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "DATA-004",
            Summary = $"Counter overflow detected for device '{deviceId}' channel {channel}",
            DetailedDescription = $"Counter value rolled over from {previousValue} to {currentValue} (max: {maxValue})",
            TroubleshootingSteps = new List<string>
            {
                $"1. EXPECTED BEHAVIOR: Counter overflow is normal for high-frequency counting",
                $"2. RATE CALCULATION: System will automatically adjust rate calculations",
                $"3. MONITORING: Monitor for unexpected multiple overflows in short time",
                $"4. COUNTER SIZE: Consider using larger counter size if overflows are too frequent",
                $"5. RESET FREQUENCY: Evaluate if counter reset frequency needs adjustment",
                $"6. DATA INTEGRITY: Verify overflow handling is working correctly",
                $"7. LOGGING: Check logs for proper overflow handling"
            },
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = deviceId,
                ["Channel"] = channel,
                ["CurrentValue"] = currentValue,
                ["PreviousValue"] = previousValue,
                ["MaxValue"] = maxValue,
                ["OverflowAmount"] = currentValue + (maxValue - previousValue),
                ["CounterType"] = maxValue == uint.MaxValue ? "32-bit" : "16-bit"
            },
            EscalationProcedure = "Contact system administrator if overflow frequency is unexpectedly high or data integrity is compromised.",
            Severity = ErrorSeverity.Info,
            Category = ErrorCategory.Data,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            DeviceId = deviceId,
            Channel = channel,
            TechnicalDetails = $"Counter overflow: Current={currentValue}, Previous={previousValue}, Max={maxValue}"
        };
    }

    #endregion

    #region Configuration Errors

    /// <summary>
    /// Create error message for configuration validation failures
    /// </summary>
    /// <param name="configSection">Configuration section name</param>
    /// <param name="propertyName">Property name</param>
    /// <param name="currentValue">Current property value</param>
    /// <param name="constraint">Validation constraint</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreateConfigurationValidationFailure(
        string configSection,
        string propertyName,
        object currentValue,
        string constraint,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "CONF-004",
            Summary = $"Configuration validation failed in section '{configSection}'",
            DetailedDescription = $"Property '{propertyName}' with value '{currentValue}' violates constraint: {constraint}",
            TroubleshootingSteps = new List<string>
            {
                $"1. REVIEW VALUE: Check if '{currentValue}' is the intended value for {propertyName}",
                $"2. CONSTRAINT CHECK: Understand the constraint: {constraint}",
                $"3. VALID RANGE: Consult documentation for valid range of {propertyName}",
                $"4. DEPENDENCY CHECK: Verify this property doesn't conflict with other settings",
                $"5. ENVIRONMENT: Consider if this value is appropriate for current environment",
                $"6. BACKUP CONFIG: Compare with last known good configuration",
                $"7. VALIDATION: Use configuration validation tool to check entire config"
            },
            Context = new Dictionary<string, object>
            {
                ["ConfigSection"] = configSection,
                ["PropertyName"] = propertyName,
                ["CurrentValue"] = currentValue,
                ["Constraint"] = constraint,
                ["ConfigFile"] = "appsettings.json",
                ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            },
            EscalationProcedure = "Contact system administrator if configuration requirements are unclear. Provide current configuration file and deployment environment details.",
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.Configuration,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            TechnicalDetails = $"Configuration validation failed: Section={configSection}, Property={propertyName}, Value={currentValue}, Constraint={constraint}"
        };
    }

    #endregion

    #region Performance Errors

    /// <summary>
    /// Create error message for performance degradation warnings
    /// </summary>
    /// <param name="metricName">Performance metric name</param>
    /// <param name="currentValue">Current metric value</param>
    /// <param name="threshold">Performance threshold</param>
    /// <param name="recommendation">Performance recommendation</param>
    /// <param name="memberName">Calling member name</param>
    /// <param name="sourceFilePath">Source file path</param>
    /// <param name="sourceLineNumber">Source line number</param>
    /// <returns>Industrial error message</returns>
    public static IndustrialErrorMessage CreatePerformanceDegradation(
        string metricName,
        double currentValue,
        double threshold,
        string recommendation,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        return new IndustrialErrorMessage
        {
            ErrorCode = "PERF-005",
            Summary = $"Performance degradation detected: {metricName}",
            DetailedDescription = $"Current {metricName} value {currentValue:F2} exceeds threshold {threshold:F2}",
            TroubleshootingSteps = new List<string>
            {
                $"1. MONITOR TRENDS: Check if this is a temporary spike or sustained degradation",
                $"2. RESOURCE USAGE: Monitor CPU, memory, and network utilization",
                $"3. DEVICE LOAD: Check if too many devices are being polled simultaneously",
                $"4. NETWORK LATENCY: Test network latency to devices",
                $"5. POLLING INTERVAL: Consider increasing polling interval if system is overloaded",
                $"6. BATCH SIZE: Optimize batch processing configuration",
                $"7. SYSTEM HEALTH: Check overall system health and available resources"
            },
            Context = new Dictionary<string, object>
            {
                ["MetricName"] = metricName,
                ["CurrentValue"] = currentValue,
                ["Threshold"] = threshold,
                ["Recommendation"] = recommendation,
                ["SystemLoad"] = Environment.ProcessorCount,
                ["AvailableMemory"] = GC.GetTotalMemory(false),
                ["ThreadCount"] = System.Threading.ThreadPool.ThreadCount
            },
            EscalationProcedure = "If performance issues persist, contact system administrator. Provide system performance metrics and configuration details.",
            Severity = ErrorSeverity.Low,
            Category = ErrorCategory.Performance,
            Source = new ErrorSource(memberName, sourceFilePath, sourceLineNumber),
            TechnicalDetails = $"Performance degradation: Metric={metricName}, Current={currentValue:F2}, Threshold={threshold:F2}"
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get Modbus function code for operation
    /// </summary>
    /// <param name="operation">Modbus operation name</param>
    /// <returns>Function code description</returns>
    private static string GetModbusFunctionCode(string operation)
    {
        return operation.ToLower() switch
        {
            "read holding registers" => "Read Holding Registers (0x03)",
            "read input registers" => "Read Input Registers (0x04)",
            "read coils" => "Read Coils (0x01)",
            "read discrete inputs" => "Read Discrete Inputs (0x02)",
            "write single coil" => "Write Single Coil (0x05)",
            "write single register" => "Write Single Register (0x06)",
            "write multiple coils" => "Write Multiple Coils (0x0F)",
            "write multiple registers" => "Write Multiple Registers (0x10)",
            _ => "Unknown Function"
        };
    }

    #endregion
}
