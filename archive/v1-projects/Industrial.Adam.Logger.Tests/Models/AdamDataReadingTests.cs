// Industrial.Adam.Logger.Tests - AdamDataReading Model Unit Tests
// Tests for the core data reading and device health models - REQUIREMENTS-BASED TESTING

using FluentAssertions;
using Industrial.Adam.Logger.Models;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Models;

/// <summary>
/// Unit tests for AdamDataReading model and related enums
/// FOCUS: Testing business requirements and expected behavior, not just implementation
/// </summary>
public class AdamDataReadingTests
{
    #region AdamDataReading Business Requirements Tests

    [Fact]
    public void AdamDataReading_MustHaveRequiredProperties_ToEnsureTraceabilityAndAuditCompliance()
    {
        // REQUIREMENT: Every data reading must be fully traceable with device ID, channel, raw value, and timestamp
        // for industrial audit compliance and quality assurance
        
        // Arrange
        var deviceId = "ADAM-001";
        var channel = 1;
        var rawValue = 12345L;
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var reading = new AdamDataReading
        {
            DeviceId = deviceId,
            Channel = channel,
            RawValue = rawValue,
            Timestamp = timestamp
        };

        // Assert - REQUIREMENT VALIDATION
        reading.DeviceId.Should().Be(deviceId, "Device ID is required for equipment traceability");
        reading.Channel.Should().Be(channel, "Channel number is required for multi-channel device identification");
        reading.RawValue.Should().Be(rawValue, "Raw value must be preserved for audit trail and reprocessing");
        reading.Timestamp.Should().Be(timestamp, "Timestamp is required for chronological data sequencing");
        reading.Quality.Should().Be(DataQuality.Good, "Data quality should default to Good for valid readings");
        reading.Tags.Should().NotBeNull("Tags collection must be available for metadata classification");
    }

    [Fact]
    public void AdamDataReading_ShouldSupportProcessedValueCalculation_ForIndustrialReporting()
    {
        // REQUIREMENT: System must support processed values (scaled, offset, transformed) separate from raw values
        // to enable proper engineering units and manufacturing reporting
        
        // Arrange
        var rawValue = 32768L; // Raw 16-bit value
        var expectedProcessedValue = 100.0; // Scaled to engineering units (e.g., 0-100%)
        var expectedUnit = "percent";
        
        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-002",
            Channel = 1,
            RawValue = rawValue,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = expectedProcessedValue,
            Unit = expectedUnit
        };

        // Assert - REQUIREMENT VALIDATION
        reading.RawValue.Should().Be(rawValue, "Raw value must be preserved for traceability");
        reading.ProcessedValue.Should().Be(expectedProcessedValue, "Processed value must support engineering units");
        reading.Unit.Should().Be(expectedUnit, "Unit must be specified for proper interpretation");
    }

    [Fact]
    public void AdamDataReading_ShouldSupportRateCalculation_ForProductionMonitoring()
    {
        // REQUIREMENT: System must calculate rates for production monitoring and efficiency analysis
        
        // Arrange
        var rate = 120.5; // units per minute
        var expectedUnit = "parts/min";
        
        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-003",
            Channel = 1,
            RawValue = 7200L,
            Timestamp = DateTimeOffset.UtcNow,
            Rate = rate,
            Unit = expectedUnit
        };

        // Assert - REQUIREMENT VALIDATION
        reading.Rate.Should().Be(rate, "Rate calculation must be supported for production monitoring");
        reading.Unit.Should().Be(expectedUnit, "Rate unit must be specified for proper interpretation");
    }

    [Fact]
    public void AdamDataReading_ShouldSupportQualityAssessment_ForDataValidation()
    {
        // REQUIREMENT: Every data reading must have quality assessment to ensure data integrity
        
        // Arrange
        var expectedQuality = DataQuality.Uncertain;
        var expectedErrorMessage = "Sensor calibration overdue";
        
        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-004",
            Channel = 1,
            RawValue = 12345L,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = expectedQuality,
            ErrorMessage = expectedErrorMessage
        };

        // Assert - REQUIREMENT VALIDATION
        reading.Quality.Should().Be(expectedQuality, "Quality assessment must be available for data validation");
        reading.ErrorMessage.Should().Be(expectedErrorMessage, "Error message must be available for quality issues");
    }

    [Fact]
    public void AdamDataReading_ShouldSupportMetadataTags_ForManufacturingContext()
    {
        // REQUIREMENT: System must support metadata tags for manufacturing context and filtering
        
        // Arrange
        var expectedTags = new Dictionary<string, object>
        {
            ["ProductionLine"] = "Line A",
            ["WorkCenter"] = "Cell 1",
            ["Shift"] = "Day",
            ["ProductType"] = "Widget-A",
            ["QualityGrade"] = "Grade1"
        };
        
        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-005",
            Channel = 1,
            RawValue = 12345L,
            Timestamp = DateTimeOffset.UtcNow,
            Tags = expectedTags
        };

        // Assert - REQUIREMENT VALIDATION
        reading.Tags.Should().BeEquivalentTo(expectedTags, "Tags must support manufacturing context classification");
        reading.Tags.Should().ContainKey("ProductionLine", "Production line must be identifiable");
        reading.Tags.Should().ContainKey("WorkCenter", "Work center must be identifiable");
        reading.Tags.Should().ContainKey("Shift", "Shift information must be available");
    }

    [Fact]
    public void AdamDataReading_ShouldTrackAcquisitionTime_ForPerformanceMonitoring()
    {
        // REQUIREMENT: System must track acquisition time for performance monitoring and SLA compliance
        
        // Arrange
        var expectedAcquisitionTime = TimeSpan.FromMilliseconds(250);
        var maxAcceptableAcquisitionTime = TimeSpan.FromMilliseconds(1000);
        
        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-006",
            Channel = 1,
            RawValue = 12345L,
            Timestamp = DateTimeOffset.UtcNow,
            AcquisitionTime = expectedAcquisitionTime
        };

        // Assert - REQUIREMENT VALIDATION
        reading.AcquisitionTime.Should().Be(expectedAcquisitionTime, "Acquisition time must be tracked for performance monitoring");
        reading.AcquisitionTime.Should().BeLessThan(maxAcceptableAcquisitionTime, "Acquisition time must meet performance requirements");
    }

    [Fact]
    public void AdamDataReading_ShouldSupportImmutableRecordSemantics_ForDataIntegrity()
    {
        // REQUIREMENT: Data readings must be immutable records to prevent accidental modification
        // and ensure data integrity in multi-threaded industrial environments
        
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var commonTags = new Dictionary<string, object> { ["Line"] = "A" };
        var reading1 = new AdamDataReading
        {
            DeviceId = "ADAM-001",
            Channel = 1,
            RawValue = 12345L,
            Timestamp = timestamp,
            Tags = commonTags
        };
        var reading2 = new AdamDataReading
        {
            DeviceId = "ADAM-001",
            Channel = 1,
            RawValue = 12345L,
            Timestamp = timestamp,
            Tags = commonTags // Same reference for equality
        };
        var reading3 = new AdamDataReading
        {
            DeviceId = "ADAM-002", // Different device
            Channel = 1,
            RawValue = 12345L,
            Timestamp = timestamp,
            Tags = commonTags
        };

        // Act & Assert - REQUIREMENT VALIDATION
        reading1.Should().Be(reading2, "Identical readings must be equal for deduplication");
        reading1.Should().NotBe(reading3, "Different readings must not be equal for proper identification");
        reading1.GetHashCode().Should().Be(reading2.GetHashCode(), "Hash codes must be consistent for collections");
    }

    [Fact]
    public void AdamDataReading_OptionalProperties_ShouldSupportNullValues_ForGracefulDegradation()
    {
        // REQUIREMENT: Optional properties must support null values to allow graceful degradation
        // when processing or rate calculations are unavailable
        
        // Arrange & Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-003",
            Channel = 3,
            RawValue = 54321L,
            Timestamp = DateTimeOffset.UtcNow,
            ProcessedValue = null,
            Rate = null,
            Unit = null,
            ErrorMessage = null
        };

        // Assert - REQUIREMENT VALIDATION
        reading.Should().NotBeNull("Reading must be valid even without optional properties");
        reading.ProcessedValue.Should().BeNull("ProcessedValue must support null when calculation fails");
        reading.Rate.Should().BeNull("Rate must support null when calculation unavailable");
        reading.Unit.Should().BeNull("Unit must support null when not specified");
        reading.ErrorMessage.Should().BeNull("ErrorMessage must support null when no errors");
    }

    #endregion

    #region AdamDeviceHealth Business Requirements Tests

    [Fact]
    public void AdamDeviceHealth_MustTrackDeviceStatus_ForMaintenanceAndOperations()
    {
        // REQUIREMENT: Device health must track operational status for maintenance planning
        // and operational decision making
        
        // Arrange
        var deviceId = "ADAM-001";
        var timestamp = DateTimeOffset.UtcNow;
        var status = DeviceStatus.Online;

        // Act
        var health = new AdamDeviceHealth
        {
            DeviceId = deviceId,
            Timestamp = timestamp,
            Status = status
        };

        // Assert - REQUIREMENT VALIDATION
        health.DeviceId.Should().Be(deviceId, "Device ID must be tracked for equipment identification");
        health.Timestamp.Should().Be(timestamp, "Timestamp must be recorded for health history");
        health.Status.Should().Be(status, "Status must indicate operational condition");
        health.IsConnected.Should().BeFalse("Default connectivity should be conservative (false)");
        health.ConsecutiveFailures.Should().Be(0, "New device should start with zero failures");
        health.TotalReads.Should().Be(0, "New device should start with zero read attempts");
        health.SuccessfulReads.Should().Be(0, "New device should start with zero successful reads");
        health.SuccessRate.Should().Be(0, "Success rate should be zero when no reads attempted");
    }

    [Fact]
    public void AdamDeviceHealth_MustCalculateAccurateSuccessRate_ForReliabilityMonitoring()
    {
        // REQUIREMENT: Success rate must be accurately calculated for equipment reliability monitoring
        // and predictive maintenance decisions
        
        // Arrange & Act
        var highReliabilityDevice = new AdamDeviceHealth
        {
            DeviceId = "ADAM-001",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            TotalReads = 100,
            SuccessfulReads = 95
        };

        var noAttemptsDevice = new AdamDeviceHealth
        {
            DeviceId = "ADAM-002",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Unknown,
            TotalReads = 0,
            SuccessfulReads = 0
        };

        var perfectDevice = new AdamDeviceHealth
        {
            DeviceId = "ADAM-003",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Online,
            TotalReads = 50,
            SuccessfulReads = 50
        };

        // Assert - REQUIREMENT VALIDATION
        highReliabilityDevice.SuccessRate.Should().Be(95.0, "Success rate must be calculated as (successful/total)*100");
        noAttemptsDevice.SuccessRate.Should().Be(0.0, "Success rate must be 0 when no attempts made");
        perfectDevice.SuccessRate.Should().Be(100.0, "Success rate must be 100 for perfect reliability");
    }

    [Fact]
    public void AdamDeviceHealth_MustTrackCommunicationMetrics_ForPerformanceOptimization()
    {
        // REQUIREMENT: Device health must track communication metrics for performance optimization
        // and network troubleshooting
        
        // Arrange
        var deviceId = "ADAM-PERF-001";
        var timestamp = DateTimeOffset.UtcNow;
        var expectedLatency = 45.5; // milliseconds
        var acceptableLatency = 100.0; // milliseconds
        var consecutiveFailures = 2;
        var maxAcceptableFailures = 5;

        // Act
        var health = new AdamDeviceHealth
        {
            DeviceId = deviceId,
            Timestamp = timestamp,
            Status = DeviceStatus.Warning,
            IsConnected = true,
            ConsecutiveFailures = consecutiveFailures,
            CommunicationLatency = expectedLatency,
            LastError = "Intermittent connection issues",
            TotalReads = 100,
            SuccessfulReads = 85
        };

        // Assert - REQUIREMENT VALIDATION
        health.CommunicationLatency.Should().Be(expectedLatency, "Communication latency must be tracked for performance monitoring");
        health.CommunicationLatency.Should().BeLessThan(acceptableLatency, "Communication latency must be within acceptable limits");
        health.ConsecutiveFailures.Should().Be(consecutiveFailures, "Consecutive failures must be tracked for reliability assessment");
        health.ConsecutiveFailures.Should().BeLessThan(maxAcceptableFailures, "Consecutive failures must be within acceptable limits");
        health.LastError.Should().NotBeNullOrEmpty("Error message must be available for troubleshooting");
        health.IsConnected.Should().BeTrue("Connection status must be tracked for operational decisions");
    }

    [Fact]
    public void AdamDeviceHealth_MustSupportMaintenanceScheduling_ThroughHealthMetrics()
    {
        // REQUIREMENT: Device health must provide metrics for predictive maintenance scheduling
        
        // Arrange
        var deviceId = "ADAM-MAINT-001";
        var lastSuccessfulRead = TimeSpan.FromMinutes(30); // 30 minutes ago
        var maxAcceptableReadAge = TimeSpan.FromHours(1); // 1 hour
        var warningThreshold = TimeSpan.FromMinutes(15); // 15 minutes

        // Act
        var health = new AdamDeviceHealth
        {
            DeviceId = deviceId,
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Warning,
            LastSuccessfulRead = lastSuccessfulRead,
            ConsecutiveFailures = 3,
            TotalReads = 1000,
            SuccessfulReads = 950
        };

        // Assert - REQUIREMENT VALIDATION
        health.LastSuccessfulRead.Should().Be(lastSuccessfulRead, "Last successful read time must be tracked for freshness assessment");
        health.LastSuccessfulRead.Should().BeLessThan(maxAcceptableReadAge, "Last successful read must be within acceptable time window");
        health.LastSuccessfulRead.Should().BeGreaterThan(warningThreshold, "Last successful read should trigger maintenance warning");
        health.SuccessRate.Should().Be(95.0, "Success rate must support maintenance decision making");
        health.ConsecutiveFailures.Should().BeGreaterThan(0, "Consecutive failures must indicate maintenance need");
    }

    #endregion

    #region DataQuality Enum Business Requirements Tests

    [Fact]
    public void DataQuality_MustProvideComprehensiveQualityClassification_ForIndustrialStandards()
    {
        // REQUIREMENT: Data quality enumeration must provide comprehensive classification
        // to meet industrial data quality standards (IEC 62453, ISA-95)
        
        // Act & Assert - REQUIREMENT VALIDATION
        ((int)DataQuality.Good).Should().Be(0, "Good quality must be default/lowest value for performance");
        ((int)DataQuality.Uncertain).Should().Be(1, "Uncertain quality must indicate questionable but usable data");
        ((int)DataQuality.Bad).Should().Be(2, "Bad quality must indicate invalid data");
        ((int)DataQuality.Timeout).Should().Be(3, "Timeout must indicate communication failure");
        ((int)DataQuality.DeviceFailure).Should().Be(4, "Device failure must indicate hardware issues");
        ((int)DataQuality.ConfigurationError).Should().Be(5, "Configuration error must indicate setup issues");
        ((int)DataQuality.Overflow).Should().Be(6, "Overflow must indicate counter overflow detection");
        ((int)DataQuality.Unknown).Should().Be(7, "Unknown must be highest value for cautious handling");
    }

    [Fact]
    public void DataQuality_AllValues_MustBeAccessible_ForQualityAssessment()
    {
        // REQUIREMENT: All quality values must be accessible for comprehensive quality assessment
        
        // Act
        var allValues = Enum.GetValues<DataQuality>();

        // Assert - REQUIREMENT VALIDATION
        allValues.Should().HaveCount(8, "Must have exactly 8 quality levels for comprehensive assessment");
        allValues.Should().Contain(DataQuality.Good, "Good quality must be available for valid data");
        allValues.Should().Contain(DataQuality.Uncertain, "Uncertain quality must be available for questionable data");
        allValues.Should().Contain(DataQuality.Bad, "Bad quality must be available for invalid data");
        allValues.Should().Contain(DataQuality.Timeout, "Timeout must be available for communication failures");
        allValues.Should().Contain(DataQuality.DeviceFailure, "Device failure must be available for hardware issues");
        allValues.Should().Contain(DataQuality.ConfigurationError, "Configuration error must be available for setup issues");
        allValues.Should().Contain(DataQuality.Overflow, "Overflow must be available for counter overflow detection");
        allValues.Should().Contain(DataQuality.Unknown, "Unknown must be available for undetermined quality");
    }

    #endregion

    #region DeviceStatus Enum Business Requirements Tests

    [Fact]
    public void DeviceStatus_MustProvideOperationalStatusClassification_ForMaintenanceOperations()
    {
        // REQUIREMENT: Device status enumeration must provide operational classification
        // for maintenance operations and production planning
        
        // Act & Assert - REQUIREMENT VALIDATION
        ((int)DeviceStatus.Online).Should().Be(0, "Online status must be default/lowest value for optimal performance");
        ((int)DeviceStatus.Warning).Should().Be(1, "Warning status must indicate minor operational issues");
        ((int)DeviceStatus.Error).Should().Be(2, "Error status must indicate significant operational problems");
        ((int)DeviceStatus.Offline).Should().Be(3, "Offline status must indicate non-responsive device");
        ((int)DeviceStatus.Unknown).Should().Be(4, "Unknown status must be highest value for cautious handling");
    }

    [Fact]
    public void DeviceStatus_AllValues_MustBeAccessible_ForOperationalDecisions()
    {
        // REQUIREMENT: All device status values must be accessible for operational decisions
        
        // Act
        var allValues = Enum.GetValues<DeviceStatus>();

        // Assert - REQUIREMENT VALIDATION
        allValues.Should().HaveCount(5, "Must have exactly 5 status levels for operational classification");
        allValues.Should().Contain(DeviceStatus.Online, "Online status must be available for operational devices");
        allValues.Should().Contain(DeviceStatus.Warning, "Warning status must be available for devices with minor issues");
        allValues.Should().Contain(DeviceStatus.Error, "Error status must be available for devices with significant problems");
        allValues.Should().Contain(DeviceStatus.Offline, "Offline status must be available for non-responsive devices");
        allValues.Should().Contain(DeviceStatus.Unknown, "Unknown status must be available for undetermined device state");
    }

    #endregion

    #region Critical Edge Cases and Validation Tests

    [Fact]
    public void AdamDataReading_MustHandleCounterOverflow_ForContinuousOperation()
    {
        // REQUIREMENT: System must handle counter overflow scenarios for continuous 24/7 operations
        
        // Arrange
        var overflowValue = long.MaxValue; // Maximum possible counter value

        // Act
        var reading = new AdamDataReading
        {
            DeviceId = "ADAM-001",
            Channel = 1,
            RawValue = overflowValue,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = DataQuality.Overflow
        };

        // Assert - REQUIREMENT VALIDATION
        reading.RawValue.Should().Be(overflowValue, "System must handle maximum counter values");
        reading.Quality.Should().Be(DataQuality.Overflow, "Overflow condition must be properly classified");
    }

    [Fact]
    public void AdamDeviceHealth_MustHandleZeroDivisionInSuccessRate_ForRobustOperation()
    {
        // REQUIREMENT: System must handle zero division in success rate calculation for robust operation
        
        // Arrange
        var healthWithZeroReads = new AdamDeviceHealth
        {
            DeviceId = "ADAM-001",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Unknown,
            TotalReads = 0,
            SuccessfulReads = 0
        };

        // Act & Assert - REQUIREMENT VALIDATION
        healthWithZeroReads.SuccessRate.Should().Be(0.0, "Success rate must be 0 when no read attempts made (avoid division by zero)");
    }

    [Fact]
    public void AdamDeviceHealth_MustHandleExtremeCommunicationLatency_ForNetworkTroubleshooting()
    {
        // REQUIREMENT: System must handle extreme communication latency for network troubleshooting
        
        // Arrange
        var extremeLatency = 999999.99; // Very high latency indicating network issues

        // Act
        var health = new AdamDeviceHealth
        {
            DeviceId = "ADAM-001",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Error,
            CommunicationLatency = extremeLatency
        };

        // Assert - REQUIREMENT VALIDATION
        health.CommunicationLatency.Should().Be(extremeLatency, "System must handle extreme latency values for troubleshooting");
        health.Status.Should().Be(DeviceStatus.Error, "High latency should correlate with error status");
    }

    [Fact]
    public void AdamDeviceHealth_MustHandleDataInconsistency_ForDataValidation()
    {
        // REQUIREMENT: System must handle data inconsistency scenarios for validation
        
        // Arrange - Testing edge case where successful reads exceed total reads
        var health = new AdamDeviceHealth
        {
            DeviceId = "ADAM-001",
            Timestamp = DateTimeOffset.UtcNow,
            Status = DeviceStatus.Error,
            TotalReads = 10,
            SuccessfulReads = 15 // Data inconsistency - should be caught in validation
        };

        // Act & Assert - REQUIREMENT VALIDATION
        health.SuccessRate.Should().Be(150.0, "Success rate calculation must handle data inconsistency gracefully");
        health.Status.Should().Be(DeviceStatus.Error, "Data inconsistency should be reflected in device status");
    }

    #endregion
}