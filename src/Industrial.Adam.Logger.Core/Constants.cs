namespace Industrial.Adam.Logger.Core;

/// <summary>
/// Core application constants for industrial ADAM device communication
/// </summary>
public static class Constants
{
    #region Modbus Protocol
    
    /// <summary>
    /// Default Modbus TCP port
    /// </summary>
    public const int DefaultModbusPort = 502;
    
    /// <summary>
    /// Number of registers required for a 32-bit counter value
    /// </summary>
    public const int CounterRegisterCount = 2;
    
    /// <summary>
    /// Maximum value for a 32-bit unsigned integer
    /// </summary>
    public const long UInt32MaxValue = 4_294_967_295L;
    
    /// <summary>
    /// Default overflow threshold for 32-bit counters (near max value)
    /// </summary>
    public const long DefaultOverflowThreshold = 4_294_000_000L;
    
    #endregion
    
    #region Timing and Retry
    
    /// <summary>
    /// Default device polling interval in milliseconds
    /// </summary>
    public const int DefaultPollIntervalMs = 1000;
    
    /// <summary>
    /// Default device timeout in milliseconds
    /// </summary>
    public const int DefaultTimeoutMs = 3000;
    
    /// <summary>
    /// Default maximum retry attempts
    /// </summary>
    public const int DefaultMaxRetries = 3;
    
    /// <summary>
    /// Default retry delay in milliseconds
    /// </summary>
    public const int DefaultRetryDelayMs = 1000;
    
    /// <summary>
    /// Connection retry cooldown period in seconds
    /// </summary>
    public const int ConnectionRetryCooldownSeconds = 5;
    
    /// <summary>
    /// Default health check interval in milliseconds
    /// </summary>
    public const int DefaultHealthCheckIntervalMs = 30000;
    
    #endregion
    
    #region Validation Limits
    
    /// <summary>
    /// Maximum consecutive failures before marking device offline
    /// </summary>
    public const int MaxConsecutiveFailures = 5;
    
    /// <summary>
    /// Minimum valid TCP port number
    /// </summary>
    public const int MinPortNumber = 1;
    
    /// <summary>
    /// Maximum valid TCP port number
    /// </summary>
    public const int MaxPortNumber = 65535;
    
    /// <summary>
    /// Minimum Modbus unit ID
    /// </summary>
    public const byte MinModbusUnitId = 1;
    
    /// <summary>
    /// Maximum Modbus unit ID
    /// </summary>
    public const byte MaxModbusUnitId = 255;
    
    #endregion
    
    #region Buffer and Performance
    
    /// <summary>
    /// Default TCP receive buffer size in bytes
    /// </summary>
    public const int DefaultReceiveBufferSize = 8192;
    
    /// <summary>
    /// Default TCP send buffer size in bytes
    /// </summary>
    public const int DefaultSendBufferSize = 8192;
    
    /// <summary>
    /// Default batch size for InfluxDB writes
    /// </summary>
    public const int DefaultBatchSize = 100;
    
    /// <summary>
    /// Default batch timeout in milliseconds
    /// </summary>
    public const int DefaultBatchTimeoutMs = 5000;
    
    #endregion
    
    #region Data Quality
    
    /// <summary>
    /// Default rate calculation window in seconds
    /// </summary>
    public const int DefaultRateWindowSeconds = 60;
    
    /// <summary>
    /// High rate threshold for anomaly detection (units per second)
    /// </summary>
    public const double DefaultHighRateThreshold = 1000.0;
    
    #endregion
}