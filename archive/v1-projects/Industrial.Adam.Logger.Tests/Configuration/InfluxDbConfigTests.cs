// Industrial.Adam.Logger.Tests - InfluxDbConfig Unit Tests
// Tests for InfluxDB configuration validation - REQUIREMENTS-BASED TESTING

using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Configuration;

/// <summary>
/// Unit tests for InfluxDbConfig validation
/// FOCUS: Testing business requirements and expected behavior, not just implementation
/// </summary>
public class InfluxDbConfigTests
{
    #region InfluxDbConfig Valid Configuration Tests

    [Fact]
    public void InfluxDbConfig_WithValidConfiguration_ShouldPassValidation()
    {
        // REQUIREMENT: Valid InfluxDB configuration must pass validation
        // for successful database connectivity and data storage
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "https://influxdb.example.com:8086",
            Token = "valid-token-123",
            Organization = "manufacturing-org",
            Bucket = "production-data",
            Measurement = "sensor_readings",
            WriteBatchSize = 500,
            FlushIntervalMs = 10000,
            TimeoutMs = 15000,
            EnableRetry = true,
            MaxRetryAttempts = 3,
            RetryDelayMs = 2000,
            EnableDebugLogging = false,
            EnableCompression = true,
            GlobalTags = new Dictionary<string, string> { ["environment"] = "production" }
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Valid configuration must pass validation for database connectivity");
    }

    [Fact]
    public void InfluxDbConfig_WithMinimalValidConfiguration_ShouldPassValidation()
    {
        // REQUIREMENT: Minimal valid configuration must pass validation
        // for basic database functionality
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Minimal valid configuration must pass validation for basic functionality");
    }

    [Fact]
    public void InfluxDbConfig_WithDefaultValues_ShouldPassValidation()
    {
        // REQUIREMENT: Default configuration values must be valid
        // for out-of-the-box functionality
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Token = "default-token",
            Organization = "default-org",
            Bucket = "default-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Default configuration values must be valid for out-of-the-box functionality");
        config.Url.Should().Be("http://localhost:8086", "Default URL must be localhost for development");
        config.Measurement.Should().Be("counter_data", "Default measurement name must be appropriate for counter data");
        config.WriteBatchSize.Should().Be(100, "Default batch size must be optimized for performance");
        config.FlushIntervalMs.Should().Be(5000, "Default flush interval must balance performance and data freshness");
        config.TimeoutMs.Should().Be(30000, "Default timeout must allow sufficient time for operations");
        config.EnableRetry.Should().BeTrue("Retry should be enabled by default for reliability");
        config.MaxRetryAttempts.Should().Be(3, "Default retry attempts must provide fault tolerance");
        config.RetryDelayMs.Should().Be(1000, "Default retry delay must provide reasonable backoff");
        config.EnableDebugLogging.Should().BeFalse("Debug logging should be disabled by default for performance");
        config.EnableCompression.Should().BeTrue("Compression should be enabled by default for efficiency");
        config.GlobalTags.Should().NotBeNull("Global tags collection must be initialized");
    }

    #endregion

    #region URL Validation Tests

    [Fact]
    public void InfluxDbConfig_WithEmptyUrl_ShouldFailValidation()
    {
        // REQUIREMENT: URL must be required for database connectivity
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Empty URL must be rejected for database connectivity");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("URL") && r.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void InfluxDbConfig_WithInvalidUrlFormat_ShouldFailValidation()
    {
        // REQUIREMENT: URL must be valid format for proper HTTP communication
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "not-a-valid-url",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid URL format must be rejected for HTTP communication");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("Invalid") && r.ErrorMessage!.Contains("URL"));
    }

    [Fact]
    public void InfluxDbConfig_WithUnsupportedScheme_ShouldFailValidation()
    {
        // REQUIREMENT: URL must use HTTP or HTTPS for supported protocols
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "ftp://influxdb.example.com:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Unsupported URL scheme must be rejected for protocol compatibility");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("HTTP") && r.ErrorMessage!.Contains("HTTPS"));
    }

    [Fact]
    public void InfluxDbConfig_WithHttpsUrl_ShouldPassValidation()
    {
        // REQUIREMENT: HTTPS URLs must be supported for secure communication
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "https://secure.influxdb.example.com:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("HTTPS URLs must be supported for secure communication");
    }

    [Fact]
    public void InfluxDbConfig_WithHttpUrl_ShouldPassValidation()
    {
        // REQUIREMENT: HTTP URLs must be supported for development environments
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("HTTP URLs must be supported for development environments");
    }

    #endregion

    #region Authentication Validation Tests

    [Fact]
    public void InfluxDbConfig_WithEmptyToken_ShouldFailValidation()
    {
        // REQUIREMENT: Token must be required for database authentication
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Empty token must be rejected for authentication security");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("token") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithWhitespaceToken_ShouldFailValidation()
    {
        // REQUIREMENT: Token must not be whitespace for security validation
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "   ",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Whitespace-only token must be rejected for security validation");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("token") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithEmptyOrganization_ShouldFailValidation()
    {
        // REQUIREMENT: Organization must be required for multi-tenant database access
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Empty organization must be rejected for multi-tenant access");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("organization") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithWhitespaceOrganization_ShouldFailValidation()
    {
        // REQUIREMENT: Organization must not be whitespace for access control
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "   ",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Whitespace-only organization must be rejected for access control");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("organization") && r.ErrorMessage!.Contains("empty"));
    }

    #endregion

    #region Bucket and Measurement Validation Tests

    [Fact]
    public void InfluxDbConfig_WithEmptyBucket_ShouldFailValidation()
    {
        // REQUIREMENT: Bucket must be required for data storage location
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = ""
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Empty bucket must be rejected for data storage location");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("bucket") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithWhitespaceBucket_ShouldFailValidation()
    {
        // REQUIREMENT: Bucket must not be whitespace for data storage integrity
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "   "
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Whitespace-only bucket must be rejected for data storage integrity");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("bucket") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithEmptyMeasurement_ShouldFailValidation()
    {
        // REQUIREMENT: Measurement must be required for data organization
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            Measurement = ""
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Empty measurement must be rejected for data organization");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("measurement") && r.ErrorMessage!.Contains("empty"));
    }

    [Fact]
    public void InfluxDbConfig_WithWhitespaceMeasurement_ShouldFailValidation()
    {
        // REQUIREMENT: Measurement must not be whitespace for data schema integrity
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            Measurement = "   "
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Whitespace-only measurement must be rejected for data schema integrity");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("measurement") && r.ErrorMessage!.Contains("empty"));
    }

    #endregion

    #region Performance Configuration Tests

    [Fact]
    public void InfluxDbConfig_WithInvalidWriteBatchSize_ShouldFailValidation()
    {
        // REQUIREMENT: Write batch size must be within valid range for performance optimization
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            WriteBatchSize = 0 // Invalid
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid write batch size must be rejected for performance optimization");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("WriteBatchSize") && r.ErrorMessage!.Contains("between 1 and 10,000"));
    }

    [Fact]
    public void InfluxDbConfig_WithMaxValidWriteBatchSize_ShouldPassValidation()
    {
        // REQUIREMENT: Maximum valid write batch size must be supported for high-throughput scenarios
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            WriteBatchSize = 10000 // Maximum valid
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Maximum valid write batch size must be supported for high-throughput scenarios");
    }

    [Fact]
    public void InfluxDbConfig_WithInvalidFlushInterval_ShouldFailValidation()
    {
        // REQUIREMENT: Flush interval must be within valid range for data freshness and performance
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            FlushIntervalMs = 500 // Invalid - too short
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid flush interval must be rejected for data freshness and performance");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("FlushIntervalMs") && r.ErrorMessage!.Contains("between 1 second and 5 minutes"));
    }

    [Fact]
    public void InfluxDbConfig_WithInvalidTimeout_ShouldFailValidation()
    {
        // REQUIREMENT: Timeout must be within valid range for reliable operations
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            TimeoutMs = 70000 // Invalid - too long
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid timeout must be rejected for reliable operations");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("TimeoutMs") && r.ErrorMessage!.Contains("between 1 second and 1 minute"));
    }

    #endregion

    #region Retry Configuration Tests

    [Fact]
    public void InfluxDbConfig_WithInvalidMaxRetryAttempts_ShouldFailValidation()
    {
        // REQUIREMENT: Max retry attempts must be within valid range for fault tolerance
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            MaxRetryAttempts = 0 // Invalid
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid max retry attempts must be rejected for fault tolerance");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("MaxRetryAttempts") && r.ErrorMessage!.Contains("between 1 and 10"));
    }

    [Fact]
    public void InfluxDbConfig_WithInvalidRetryDelay_ShouldFailValidation()
    {
        // REQUIREMENT: Retry delay must be within valid range for balanced retry strategy
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            RetryDelayMs = 50 // Invalid - too short
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Invalid retry delay must be rejected for balanced retry strategy");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("RetryDelayMs") && r.ErrorMessage!.Contains("between 100ms and 10 seconds"));
    }

    [Fact]
    public void InfluxDbConfig_WithDisabledRetry_ShouldRespectRetrySettings()
    {
        // REQUIREMENT: Retry can be disabled for specific use cases
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            EnableRetry = false
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Retry can be disabled for specific use cases");
        config.EnableRetry.Should().BeFalse("Retry settings must be respected when disabled");
    }

    #endregion

    #region Global Tags Configuration Tests

    [Fact]
    public void InfluxDbConfig_WithGlobalTags_ShouldSupportMetadata()
    {
        // REQUIREMENT: Global tags must be supported for consistent metadata across all data points
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            GlobalTags = new Dictionary<string, string>
            {
                ["environment"] = "production",
                ["region"] = "us-west-2",
                ["facility"] = "plant-01"
            }
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Global tags must be supported for consistent metadata");
        config.GlobalTags.Should().HaveCount(3, "Global tags must be preserved for metadata consistency");
        config.GlobalTags.Should().ContainKey("environment", "Environment tag must be supported");
        config.GlobalTags.Should().ContainKey("region", "Region tag must be supported");
        config.GlobalTags.Should().ContainKey("facility", "Facility tag must be supported");
    }

    [Fact]
    public void InfluxDbConfig_WithEmptyGlobalTags_ShouldBeValid()
    {
        // REQUIREMENT: Empty global tags must be supported for minimal configurations
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            GlobalTags = new Dictionary<string, string>()
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Empty global tags must be supported for minimal configurations");
        config.GlobalTags.Should().BeEmpty("Empty global tags must be preserved");
    }

    #endregion

    #region Feature Flag Configuration Tests

    [Fact]
    public void InfluxDbConfig_WithDebugLoggingEnabled_ShouldSupportDebugging()
    {
        // REQUIREMENT: Debug logging must be configurable for troubleshooting
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            EnableDebugLogging = true
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Debug logging must be configurable for troubleshooting");
        config.EnableDebugLogging.Should().BeTrue("Debug logging setting must be respected");
    }

    [Fact]
    public void InfluxDbConfig_WithCompressionDisabled_ShouldSupportLowLatency()
    {
        // REQUIREMENT: Compression must be configurable for performance optimization
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "test-token",
            Organization = "test-org",
            Bucket = "test-bucket",
            EnableCompression = false
        };

        // Act
        var validationResults = config.Validate();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().BeEmpty("Compression must be configurable for performance optimization");
        config.EnableCompression.Should().BeFalse("Compression setting must be respected for low-latency scenarios");
    }

    #endregion

    #region Multiple Validation Errors Tests

    [Fact]
    public void InfluxDbConfig_WithMultipleValidationErrors_ShouldReportAllErrors()
    {
        // REQUIREMENT: Multiple validation errors must be reported for comprehensive feedback
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "invalid-url",
            Token = "",
            Organization = "",
            Bucket = "",
            WriteBatchSize = 0,
            FlushIntervalMs = 100,
            MaxRetryAttempts = 0
        };

        // Act
        var validationResults = config.Validate().ToList();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().HaveCountGreaterOrEqualTo(7, "Multiple validation errors must be reported comprehensively");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("URL"), "URL validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("token"), "Token validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("organization"), "Organization validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("bucket"), "Bucket validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("WriteBatchSize"), "WriteBatchSize validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("FlushIntervalMs"), "FlushIntervalMs validation error must be reported");
        validationResults.Should().Contain(r => r.ErrorMessage!.Contains("MaxRetryAttempts"), "MaxRetryAttempts validation error must be reported");
    }

    [Fact]
    public void InfluxDbConfig_WithValidationContext_ShouldProvideDetailedErrors()
    {
        // REQUIREMENT: Validation context must provide detailed error information for debugging
        
        // Arrange
        var config = new InfluxDbConfig
        {
            Url = "http://localhost:8086",
            Token = "",
            Organization = "test-org",
            Bucket = "test-bucket"
        };

        // Act
        var validationResults = config.Validate().ToList();

        // Assert - REQUIREMENT VALIDATION
        validationResults.Should().NotBeEmpty("Validation context must provide detailed error information");
        var tokenError = validationResults.FirstOrDefault(r => r.MemberNames.Contains("Token"));
        tokenError.Should().NotBeNull("Token error must include member name for debugging");
        tokenError!.ErrorMessage.Should().Contain("token", "Token error message must be descriptive");
    }

    #endregion
}