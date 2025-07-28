// Industrial.Adam.Logger.Tests - Constants Unit Tests
// Tests for application constants - REQUIREMENTS-BASED TESTING

using FluentAssertions;
using Industrial.Adam.Logger;
using Xunit;

namespace Industrial.Adam.Logger.Tests;

/// <summary>
/// Unit tests for Constants class and related unit constants
/// FOCUS: Testing business requirements and expected behavior, not just implementation
/// </summary>
public class ConstantsTests
{
    #region Constants Business Requirements Tests

    [Fact]
    public void ModbusConstants_MustProvideStandardProtocolValues_ForIndustrialCompliance()
    {
        // REQUIREMENT: Modbus constants must align with industrial protocol standards
        // for interoperability and compliance with Modbus specification
        
        // Assert - REQUIREMENT VALIDATION
        Constants.ModbusRegisterBits.Should().Be(16, "Modbus registers must be 16-bit as per protocol specification");
        Constants.ModbusRegisterMaxValue.Should().Be(65536, "16-bit register maximum value must be 2^16 = 65536");
        Constants.DefaultModbusPort.Should().Be(502, "Default Modbus TCP port must be 502 as per IANA standard");
        Constants.AdamHoldingRegisterStartAddress.Should().Be(40001, "ADAM holding registers must start at 40001 per device specification");
        Constants.CounterRegisterCount.Should().Be(2, "32-bit counter values require 2 16-bit registers");
    }

    [Fact]
    public void ModbusAddressRanges_MustEnforceProtocolLimits_ForValidCommunication()
    {
        // REQUIREMENT: Modbus address ranges must enforce protocol limits
        // to prevent communication errors and ensure device compatibility
        
        // Assert - REQUIREMENT VALIDATION
        Constants.MinModbusUnitId.Should().Be(1, "Minimum Modbus unit ID must be 1 (0 is reserved for broadcast)");
        Constants.MaxModbusUnitId.Should().Be(255, "Maximum Modbus unit ID must be 255 (8-bit limit)");
        Constants.MaxModbusRegisterAddress.Should().Be(65535, "Maximum register address must be 65535 (16-bit limit)");
        Constants.MaxModbusRegisterCount.Should().Be(125, "Maximum registers per read must be 125 per Modbus specification");
    }

    [Fact]
    public void NetworkConstants_MustProvideValidPortRanges_ForTcpCommunication()
    {
        // REQUIREMENT: Network constants must provide valid TCP port ranges
        // for proper network configuration and security compliance
        
        // Assert - REQUIREMENT VALIDATION
        Constants.MinPortNumber.Should().Be(1, "Minimum TCP port must be 1 (0 is reserved)");
        Constants.MaxPortNumber.Should().Be(65535, "Maximum TCP port must be 65535 (16-bit limit)");
        Constants.MinPortNumber.Should().BeLessThan(Constants.MaxPortNumber, "Port range must be valid");
    }

    [Fact]
    public void TimingConstants_MustProvideIndustrialGradeIntervals_ForReliableOperation()
    {
        // REQUIREMENT: Timing constants must provide industrial-grade intervals
        // for reliable operation in manufacturing environments
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultPollIntervalMs.Should().Be(5000, "Default poll interval must be 5 seconds for industrial stability");
        Constants.DefaultHealthCheckIntervalMs.Should().Be(30000, "Health check interval must be 30 seconds for system monitoring");
        Constants.DefaultDeviceTimeoutMs.Should().Be(3000, "Device timeout must be 3 seconds for responsive error handling");
        Constants.DefaultRetryDelayMs.Should().Be(1000, "Retry delay must be 1 second for balanced retry strategy");
        Constants.DefaultMaxRetries.Should().Be(3, "Maximum retries must be 3 for fault tolerance without excessive delays");
        
        // Validate timing relationships
        Constants.DefaultDeviceTimeoutMs.Should().BeLessThan(Constants.DefaultPollIntervalMs, "Device timeout must be less than poll interval");
        Constants.DefaultRetryDelayMs.Should().BeLessThan(Constants.DefaultDeviceTimeoutMs, "Retry delay must be less than device timeout");
    }

    [Fact]
    public void NetworkRetryConstants_MustProvideEscalatingDelays_ForNetworkResilience()
    {
        // REQUIREMENT: Network retry constants must support escalating delays
        // for network resilience and congestion management
        
        // Assert - REQUIREMENT VALIDATION
        Constants.NetworkRetryDelayMs.Should().Be(500, "Network retry delay must be 500ms for quick recovery");
        Constants.MaxNetworkRetryDelaySeconds.Should().Be(10, "Maximum network retry delay must be 10 seconds");
        Constants.MaxRetryDelaySeconds.Should().Be(30, "Maximum device retry delay must be 30 seconds");
        Constants.DefaultJitterFactor.Should().Be(0.1, "Jitter factor must be 0.1 for 10% randomization");
        
        // Validate retry relationships
        Constants.NetworkRetryDelayMs.Should().BeLessThan(Constants.DefaultRetryDelayMs, "Network retry should be faster than device retry");
        Constants.MaxNetworkRetryDelaySeconds.Should().BeLessThan(Constants.MaxRetryDelaySeconds, "Network max delay should be less than device max delay");
        Constants.DefaultJitterFactor.Should().BeInRange(0.0, 1.0, "Jitter factor must be between 0 and 1");
    }

    [Fact]
    public void BufferConstants_MustProvideOptimalSizes_ForPerformanceAndMemoryManagement()
    {
        // REQUIREMENT: Buffer constants must provide optimal sizes
        // for performance optimization and memory management
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultReceiveBufferSize.Should().Be(8192, "Receive buffer must be 8KB for optimal network performance");
        Constants.DefaultSendBufferSize.Should().Be(8192, "Send buffer must be 8KB for optimal network performance");
        Constants.DefaultDataBufferSize.Should().Be(10000, "Data buffer must support 10000 readings for batch processing");
        Constants.DefaultBatchSize.Should().Be(100, "Batch size must be 100 for balanced processing efficiency");
        Constants.DefaultMaxConcurrentDevices.Should().Be(10, "Concurrent devices must be limited to 10 for resource management");
        
        // Validate buffer relationships
        Constants.DefaultReceiveBufferSize.Should().Be(Constants.DefaultSendBufferSize, "Send and receive buffers should be equal for balanced communication");
        Constants.DefaultBatchSize.Should().BeLessThan(Constants.DefaultDataBufferSize, "Batch size must be less than data buffer capacity");
    }

    [Fact]
    public void CounterConstants_MustProvideOverflowDetection_ForContinuousOperation()
    {
        // REQUIREMENT: Counter constants must provide overflow detection
        // for continuous 24/7 industrial operation
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultOverflowThreshold.Should().Be(4_294_000_000L, "Overflow threshold must be near 32-bit maximum for early detection");
        Constants.UInt32MaxValue.Should().Be(4_294_967_295L, "32-bit max value must be 2^32 - 1");
        Constants.DefaultMaxConsecutiveFailures.Should().Be(5, "Max consecutive failures must be 5 for balanced fault tolerance");
        Constants.DefaultDeviceTimeoutMinutes.Should().Be(5, "Device timeout for health monitoring must be 5 minutes");
        
        // Validate overflow relationships
        Constants.DefaultOverflowThreshold.Should().BeLessThan(Constants.UInt32MaxValue, "Overflow threshold must be less than maximum value");
        Constants.DefaultMaxConsecutiveFailures.Should().BeGreaterThan(0, "Max consecutive failures must be positive");
    }

    [Fact]
    public void ValidationConstants_MustProvideDataIntegrityLimits_ForSystemSecurity()
    {
        // REQUIREMENT: Validation constants must provide data integrity limits
        // for system security and input validation
        
        // Assert - REQUIREMENT VALIDATION
        Constants.MaxDeviceIdLength.Should().Be(50, "Device ID length must be limited to 50 characters for database compatibility");
        Constants.MaxChannelNameLength.Should().Be(100, "Channel name length must be limited to 100 characters for UI compatibility");
        Constants.DefaultDecimalPlaces.Should().Be(0, "Default decimal places must be 0 for counter values (whole numbers)");
        
        // Validate length relationships
        Constants.MaxDeviceIdLength.Should().BeGreaterThan(0, "Device ID length must be positive");
        Constants.MaxChannelNameLength.Should().BeGreaterThan(Constants.MaxDeviceIdLength, "Channel name should allow more characters than device ID");
        Constants.DefaultDecimalPlaces.Should().BeGreaterThanOrEqualTo(0, "Decimal places must be non-negative");
    }

    [Fact]
    public void PerformanceConstants_MustProvideMonitoringThresholds_ForOperationalExcellence()
    {
        // REQUIREMENT: Performance constants must provide monitoring thresholds
        // for operational excellence and predictive maintenance
        
        // Assert - REQUIREMENT VALIDATION
        Constants.HighDefectRateThreshold.Should().Be(5.0, "High defect rate threshold must be 5% for quality alerting");
        Constants.PerformanceCounterUpdateIntervalMs.Should().Be(1000, "Performance counter update interval must be 1 second");
        Constants.MaxAcquisitionCycleTimeMs.Should().Be(30000, "Maximum acquisition cycle time must be 30 seconds");
        Constants.DefaultCoefficientOfVariationThreshold.Should().Be(0.3, "CV threshold must be 0.3 for predictive maintenance");
        
        // Validate performance relationships
        Constants.HighDefectRateThreshold.Should().BeInRange(0.0, 100.0, "Defect rate threshold must be a valid percentage");
        Constants.PerformanceCounterUpdateIntervalMs.Should().BeLessThan(Constants.MaxAcquisitionCycleTimeMs, "Performance updates must be more frequent than acquisition cycles");
        Constants.DefaultCoefficientOfVariationThreshold.Should().BeGreaterThan(0.0, "CV threshold must be positive");
    }

    [Fact]
    public void OptimizationConstants_MustProvideResourceManagementLimits_ForScalability()
    {
        // REQUIREMENT: Optimization constants must provide resource management limits
        // for system scalability and performance optimization
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultRateCalculationPoints.Should().Be(120, "Rate calculation points must be 120 for 2-minute window at 1-second intervals");
        Constants.DefaultMemoryPoolSize.Should().Be(1000, "Memory pool size must be 1000 for high-frequency operations");
        Constants.DefaultConnectionPoolSize.Should().Be(5, "Connection pool size must be 5 per device for balanced resource usage");
        Constants.DefaultVectorizationBatchSize.Should().Be(64, "Vectorization batch size must be 64 for SIMD optimization");
        Constants.DefaultMaxMemoryUsageMB.Should().Be(500, "Max memory usage must be 500MB for resource constraints");
        Constants.DefaultCleanupIntervalMinutes.Should().Be(30, "Cleanup interval must be 30 minutes for memory management");
        
        // Validate optimization relationships
        Constants.DefaultRateCalculationPoints.Should().BeGreaterThan(0, "Rate calculation points must be positive");
        Constants.DefaultMemoryPoolSize.Should().BeGreaterThan(Constants.DefaultBatchSize, "Memory pool must be larger than batch size");
        Constants.DefaultConnectionPoolSize.Should().BeGreaterThan(0, "Connection pool size must be positive");
        Constants.DefaultVectorizationBatchSize.Should().BeGreaterThan(0, "Vectorization batch size must be positive");
    }

    [Fact]
    public void ThresholdConstants_MustProvidePerformanceAlerts_ForProactiveManagement()
    {
        // REQUIREMENT: Threshold constants must provide performance alerts
        // for proactive system management and SLA compliance
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultNetworkLatencyThresholdMs.Should().Be(100.0, "Network latency threshold must be 100ms for acceptable performance");
        Constants.DefaultCpuUsageThresholdPercent.Should().Be(80.0, "CPU usage threshold must be 80% for performance alerts");
        Constants.DefaultMemoryUsageThresholdPercent.Should().Be(85.0, "Memory usage threshold must be 85% for performance alerts");
        
        // Validate threshold relationships
        Constants.DefaultNetworkLatencyThresholdMs.Should().BeGreaterThan(0.0, "Network latency threshold must be positive");
        Constants.DefaultCpuUsageThresholdPercent.Should().BeInRange(0.0, 100.0, "CPU usage threshold must be a valid percentage");
        Constants.DefaultMemoryUsageThresholdPercent.Should().BeInRange(0.0, 100.0, "Memory usage threshold must be a valid percentage");
        Constants.DefaultMemoryUsageThresholdPercent.Should().BeGreaterThan(Constants.DefaultCpuUsageThresholdPercent, "Memory threshold should be higher than CPU threshold");
    }

    [Fact]
    public void TimestampFormats_MustProvideStandardFormats_ForDataExchangeInteroperability()
    {
        // REQUIREMENT: Timestamp formats must provide standard formats
        // for data exchange interoperability and international compatibility
        
        // Assert - REQUIREMENT VALIDATION
        Constants.StandardTimestampFormat.Should().Be("yyyy-MM-dd HH:mm:ss.fff", "Standard timestamp format must include milliseconds for precision");
        Constants.Iso8601TimestampFormat.Should().Be("yyyy-MM-ddTHH:mm:ss.fffZ", "ISO 8601 format must be compliant for international data exchange");
        
        // Validate format characteristics
        Constants.StandardTimestampFormat.Should().Contain("yyyy", "Year must be 4-digit for Y2K compliance");
        Constants.StandardTimestampFormat.Should().Contain("fff", "Milliseconds must be included for industrial precision");
        Constants.Iso8601TimestampFormat.Should().Contain("T", "ISO 8601 must include T separator");
        Constants.Iso8601TimestampFormat.Should().EndWith("Z", "ISO 8601 must indicate UTC timezone");
    }

    [Fact]
    public void HealthCheckEndpoints_MustProvideStandardPaths_ForMonitoringIntegration()
    {
        // REQUIREMENT: Health check endpoints must provide standard paths
        // for monitoring system integration and operational visibility
        
        // Assert - REQUIREMENT VALIDATION
        Constants.HealthCheckEndpoint.Should().Be("/health", "Health check endpoint must be /health for standard monitoring");
        Constants.ReadinessCheckEndpoint.Should().Be("/health/ready", "Readiness check must be /health/ready for Kubernetes compatibility");
        Constants.LivenessCheckEndpoint.Should().Be("/health/live", "Liveness check must be /health/live for Kubernetes compatibility");
        
        // Validate endpoint relationships
        Constants.HealthCheckEndpoint.Should().StartWith("/", "All endpoints must start with /");
        Constants.ReadinessCheckEndpoint.Should().StartWith(Constants.HealthCheckEndpoint, "Readiness endpoint must be under health path");
        Constants.LivenessCheckEndpoint.Should().StartWith(Constants.HealthCheckEndpoint, "Liveness endpoint must be under health path");
    }

    [Fact]
    public void ErrorMessageTemplates_MustProvideStructuredMessages_ForTroubleshooting()
    {
        // REQUIREMENT: Error message templates must provide structured messages
        // for effective troubleshooting and operational support
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DeviceConnectionFailureTemplate.Should().Be("Failed to connect to device {0} at {1}:{2}", "Device connection failure template must include device, host, and port");
        Constants.ConfigurationValidationErrorTemplate.Should().Be("Invalid {0} configuration: {1}", "Configuration error template must include component and details");
        Constants.DataQualityDegradationTemplate.Should().Be("Data quality degraded for device {0}, channel {1}: {2}", "Data quality template must include device, channel, and reason");
        
        // Validate template characteristics
        Constants.DeviceConnectionFailureTemplate.Should().Contain("{0}", "Device connection template must have device placeholder");
        Constants.DeviceConnectionFailureTemplate.Should().Contain("{1}", "Device connection template must have host placeholder");
        Constants.DeviceConnectionFailureTemplate.Should().Contain("{2}", "Device connection template must have port placeholder");
        Constants.ConfigurationValidationErrorTemplate.Should().Contain("{0}", "Configuration template must have component placeholder");
        Constants.ConfigurationValidationErrorTemplate.Should().Contain("{1}", "Configuration template must have details placeholder");
        Constants.DataQualityDegradationTemplate.Should().Contain("{0}", "Data quality template must have device placeholder");
        Constants.DataQualityDegradationTemplate.Should().Contain("{1}", "Data quality template must have channel placeholder");
        Constants.DataQualityDegradationTemplate.Should().Contain("{2}", "Data quality template must have reason placeholder");
    }

    #endregion

    #region DefaultUnits Business Requirements Tests

    [Fact]
    public void DefaultUnits_MustProvideStandardUnits_ForIndustrialMeasurements()
    {
        // REQUIREMENT: Default units must provide standard units
        // for consistent industrial measurements and reporting
        
        // Assert - REQUIREMENT VALIDATION
        DefaultUnits.Counts.Should().Be("counts", "Generic count unit must be 'counts' for discrete measurements");
        DefaultUnits.Parts.Should().Be("parts", "Parts unit must be 'parts' for manufacturing counting");
        DefaultUnits.UnitsPerSecond.Should().Be("units/s", "Rate unit must be 'units/s' for per-second measurements");
        DefaultUnits.PartsPerMinute.Should().Be("parts/min", "Rate unit must be 'parts/min' for manufacturing rates");
        DefaultUnits.Percentage.Should().Be("%", "Percentage unit must be '%' for ratio measurements");
    }

    [Fact]
    public void DefaultUnits_MustProvideTimeUnits_ForDurationMeasurements()
    {
        // REQUIREMENT: Default units must provide time units
        // for duration measurements and temporal analysis
        
        // Assert - REQUIREMENT VALIDATION
        DefaultUnits.Milliseconds.Should().Be("ms", "Milliseconds unit must be 'ms' for precise timing");
        DefaultUnits.Seconds.Should().Be("s", "Seconds unit must be 's' for standard timing");
        DefaultUnits.Minutes.Should().Be("min", "Minutes unit must be 'min' for longer durations");
        DefaultUnits.Hours.Should().Be("h", "Hours unit must be 'h' for extended durations");
        
        // Validate unit consistency
        DefaultUnits.Milliseconds.Should().NotBeNullOrEmpty("Milliseconds unit must be defined");
        DefaultUnits.Seconds.Should().NotBeNullOrEmpty("Seconds unit must be defined");
        DefaultUnits.Minutes.Should().NotBeNullOrEmpty("Minutes unit must be defined");
        DefaultUnits.Hours.Should().NotBeNullOrEmpty("Hours unit must be defined");
    }

    [Fact]
    public void DefaultUnits_AllUnits_MustBeAccessible_ForMeasurementClassification()
    {
        // REQUIREMENT: All default units must be accessible
        // for comprehensive measurement classification
        
        // Arrange
        var allUnits = new[]
        {
            DefaultUnits.Counts,
            DefaultUnits.Parts,
            DefaultUnits.UnitsPerSecond,
            DefaultUnits.PartsPerMinute,
            DefaultUnits.Percentage,
            DefaultUnits.Milliseconds,
            DefaultUnits.Seconds,
            DefaultUnits.Minutes,
            DefaultUnits.Hours
        };
        
        // Assert - REQUIREMENT VALIDATION
        allUnits.Should().HaveCount(9, "Must have exactly 9 standard units for comprehensive measurement");
        allUnits.Should().OnlyContain(unit => !string.IsNullOrEmpty(unit), "All units must be non-empty strings");
        allUnits.Should().OnlyHaveUniqueItems("All units must be unique for proper classification");
    }

    #endregion

    #region StandardTags Business Requirements Tests

    [Fact]
    public void StandardTags_MustProvideManufacturingContext_ForProductionTracking()
    {
        // REQUIREMENT: Standard tags must provide manufacturing context
        // for production tracking and quality management
        
        // Assert - REQUIREMENT VALIDATION
        StandardTags.ProductionLine.Should().Be("production_line", "Production line tag must be 'production_line' for manufacturing context");
        StandardTags.WorkCenter.Should().Be("work_center", "Work center tag must be 'work_center' for manufacturing cell identification");
        StandardTags.Shift.Should().Be("shift", "Shift tag must be 'shift' for work shift identification");
        StandardTags.ProductType.Should().Be("product_type", "Product type tag must be 'product_type' for product classification");
        StandardTags.QualityType.Should().Be("quality_type", "Quality type tag must be 'quality_type' for quality classification");
    }

    [Fact]
    public void StandardTags_MustProvideSystemContext_ForOperationalManagement()
    {
        // REQUIREMENT: Standard tags must provide system context
        // for operational management and monitoring
        
        // Assert - REQUIREMENT VALIDATION
        StandardTags.DeviceType.Should().Be("device_type", "Device type tag must be 'device_type' for equipment classification");
        StandardTags.AlertLevel.Should().Be("alert_level", "Alert level tag must be 'alert_level' for notification severity");
        StandardTags.DataSource.Should().Be("data_source", "Data source tag must be 'data_source' for source system identification");
        StandardTags.Environment.Should().Be("environment", "Environment tag must be 'environment' for deployment context");
        StandardTags.Location.Should().Be("location", "Location tag must be 'location' for physical facility identification");
    }

    [Fact]
    public void StandardTags_AllTags_MustFollowNamingConvention_ForConsistency()
    {
        // REQUIREMENT: All standard tags must follow naming convention
        // for consistency and compatibility with monitoring systems
        
        // Arrange
        var allTags = new[]
        {
            StandardTags.DeviceType,
            StandardTags.ProductionLine,
            StandardTags.WorkCenter,
            StandardTags.Shift,
            StandardTags.ProductType,
            StandardTags.QualityType,
            StandardTags.AlertLevel,
            StandardTags.DataSource,
            StandardTags.Environment,
            StandardTags.Location
        };
        
        // Assert - REQUIREMENT VALIDATION
        allTags.Should().HaveCount(10, "Must have exactly 10 standard tags for comprehensive classification");
        allTags.Should().OnlyContain(tag => !string.IsNullOrEmpty(tag), "All tags must be non-empty strings");
        allTags.Should().OnlyContain(tag => tag.Contains("_") || tag.All(c => char.IsLower(c)), "All tags must use snake_case naming convention or be all lowercase");
        allTags.Should().OnlyContain(tag => tag.ToLower() == tag, "All tags must be lowercase for consistency");
        allTags.Should().OnlyContain(tag => !tag.StartsWith("_") && !tag.EndsWith("_"), "Tags must not start or end with underscores");
        allTags.Should().OnlyHaveUniqueItems("All tags must be unique for proper classification");
    }

    #endregion

    #region Critical Constants Validation Tests

    [Fact]
    public void CriticalConstants_MustHaveValidValues_ForSystemStability()
    {
        // REQUIREMENT: Critical constants must have valid values
        // for system stability and operational safety
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultBatchTimeoutMs.Should().Be(5000, "Batch timeout must be 5 seconds for balanced performance");
        Constants.DefaultBatchTimeoutMs.Should().BeGreaterThan(Constants.DefaultRetryDelayMs, "Batch timeout must be greater than retry delay");
        Constants.DefaultBatchTimeoutMs.Should().BeLessThanOrEqualTo(Constants.DefaultPollIntervalMs, "Batch timeout must not exceed poll interval");
        
        // Validate critical constant relationships
        Constants.DefaultDeviceTimeoutMs.Should().BeLessThan(Constants.DefaultHealthCheckIntervalMs, "Device timeout must be less than health check interval");
        Constants.DefaultMaxRetries.Should().BeGreaterThan(0, "Max retries must be positive for fault tolerance");
        Constants.DefaultMaxRetries.Should().BeLessThan(10, "Max retries must be reasonable to prevent excessive delays");
    }

    [Fact]
    public void ConnectionRetryCooldown_MustProvideAppropriateDelay_ForNetworkStability()
    {
        // REQUIREMENT: Connection retry cooldown must provide appropriate delay
        // for network stability and connection recovery
        
        // Assert - REQUIREMENT VALIDATION
        Constants.ConnectionRetryCooldownSeconds.Should().Be(5, "Connection retry cooldown must be 5 seconds for network stability");
        Constants.ConnectionRetryCooldownSeconds.Should().BeGreaterThan(0, "Connection retry cooldown must be positive");
        Constants.ConnectionRetryCooldownSeconds.Should().BeLessThan(Constants.DefaultDeviceTimeoutMinutes * 60, "Connection retry cooldown must be less than device timeout");
    }

    [Fact]
    public void RateCalculationWindows_MustProvideConsistentTimeframes_ForAccurateMetrics()
    {
        // REQUIREMENT: Rate calculation windows must provide consistent timeframes
        // for accurate metrics and performance analysis
        
        // Assert - REQUIREMENT VALIDATION
        Constants.DefaultRateCalculationWindowMinutes.Should().Be(5, "Rate calculation window must be 5 minutes for historical data");
        Constants.DefaultRateWindowSeconds.Should().Be(60, "Rate window must be 60 seconds for configuration");
        
        // Validate rate calculation relationships
        Constants.DefaultRateCalculationWindowMinutes.Should().BeGreaterThan(0, "Rate calculation window must be positive");
        Constants.DefaultRateWindowSeconds.Should().BeGreaterThan(0, "Rate window must be positive");
        (Constants.DefaultRateCalculationWindowMinutes * 60).Should().BeGreaterThan(Constants.DefaultRateWindowSeconds, "Historical window must be larger than configuration window");
    }

    #endregion
}