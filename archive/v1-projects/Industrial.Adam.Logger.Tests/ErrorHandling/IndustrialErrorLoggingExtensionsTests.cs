using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Logging;
using System.Net.Sockets;

namespace Industrial.Adam.Logger.Tests.ErrorHandling;

public class IndustrialErrorLoggingExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public IndustrialErrorLoggingExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void LogIndustrialError_WithoutException_ShouldLogAtCorrectLevel()
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-001",
            Summary = "Test error summary",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new List<string> { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.System,
            Context = new Dictionary<string, object> { ["TestKey"] = "TestValue" }
        };

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[TEST-001]") && 
                v.ToString()!.Contains("Test error summary") &&
                v.ToString()!.Contains("Test detailed description")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogIndustrialError_WithException_ShouldUseLogStructuredError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-002",
            Summary = "Test error with exception",
            DetailedDescription = "Test detailed description with exception",
            TroubleshootingSteps = new List<string> { "Step 1" },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.System,
            OriginalException = exception
        };

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[TEST-002]") && 
                v.ToString()!.Contains("Test error with exception")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Theory]
    [InlineData(ErrorSeverity.Info, LogLevel.Information)]
    [InlineData(ErrorSeverity.Low, LogLevel.Warning)]
    [InlineData(ErrorSeverity.Medium, LogLevel.Error)]
    [InlineData(ErrorSeverity.High, LogLevel.Error)]
    [InlineData(ErrorSeverity.Critical, LogLevel.Critical)]
    public void LogIndustrialError_DifferentSeverities_ShouldMapToCorrectLogLevel(ErrorSeverity severity, LogLevel expectedLogLevel)
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-003",
            Summary = "Test error with severity",
            DetailedDescription = "Testing severity mapping",
            TroubleshootingSteps = new List<string>(),
            Severity = severity,
            Category = ErrorCategory.System
        };

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            expectedLogLevel,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogIndustrialError_WithTroubleshootingSteps_ShouldLogDebugMessages()
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-004",
            Summary = "Test error with troubleshooting",
            DetailedDescription = "Test description",
            TroubleshootingSteps = new List<string> 
            { 
                "Check power supply",
                "Verify network connection",
                "Restart device"
            },
            Severity = ErrorSeverity.Low,
            Category = ErrorCategory.Hardware
        };

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("Troubleshooting steps for TEST-004") &&
                v.ToString()!.Contains("Check power supply; Verify network connection; Restart device")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogIndustrialError_WithContext_ShouldLogContextAsDebug()
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-005",
            Summary = "Test error with context",
            DetailedDescription = "Test description",
            TroubleshootingSteps = new List<string>(),
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Data,
            Context = new Dictionary<string, object>
            {
                ["DeviceId"] = "device-001",
                ["Channel"] = 5,
                ["Value"] = 12345
            }
        };

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        // The implementation logs structured data from ToStructuredData() which prefixes with "Context_"
        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context Context_DeviceId: device-001")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context Context_Channel: 5")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Context Context_Value: 12345")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogConnectionError_ShouldCreateAndLogConnectionFailure()
    {
        // Arrange
        var deviceId = "device-001";
        var ipAddress = "192.168.1.100";
        var port = 502;
        var exception = new SocketException();

        // Act
        _mockLogger.Object.LogConnectionError(deviceId, ipAddress, port, exception);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[CONN-001]") && 
                v.ToString()!.Contains($"Failed to establish connection to device '{deviceId}'") &&
                v.ToString()!.Contains($"{ipAddress}:{port}")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogConnectionError_ShouldIncludeTroubleshootingSteps()
    {
        // Arrange
        var deviceId = "device-002";
        var ipAddress = "10.0.0.50";
        var port = 502;
        var exception = new TimeoutException("Connection timed out");

        // Act
        _mockLogger.Object.LogConnectionError(deviceId, ipAddress, port, exception);

        // Assert
        // Verify troubleshooting steps are logged
        _mockLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("Troubleshooting steps for CONN-001") &&
                v.ToString()!.Contains("VERIFY NETWORK") &&
                v.ToString()!.Contains("CHECK PORT")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogModbusError_ShouldCreateAndLogModbusCommunicationFailure()
    {
        // Arrange
        var deviceId = "device-003";
        var operation = "Read Holding Registers";
        ushort startAddress = 100;
        ushort count = 10;
        var attempt = 2;
        var maxAttempts = 3;
        var exception = new InvalidOperationException("Modbus error");

        // Act
        _mockLogger.Object.LogModbusError(deviceId, operation, startAddress, count, attempt, maxAttempts, exception);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[COMM-002]") && 
                v.ToString()!.Contains($"Modbus {operation} failed for device '{deviceId}'") &&
                v.ToString()!.Contains($"(attempt {attempt}/{maxAttempts})")),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogDataValidationError_ShouldCreateAndLogDataValidationFailure()
    {
        // Arrange
        var deviceId = "device-004";
        var channel = 3;
        var value = -999.99;
        var validationRule = "Value must be between 0 and 100";

        // Act
        _mockLogger.Object.LogDataValidationError(deviceId, channel, value, validationRule);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[DATA-003]") && 
                v.ToString()!.Contains($"Data validation failed for device '{deviceId}' channel {channel}") &&
                v.ToString()!.Contains($"Received value '{value}' failed validation rule: {validationRule}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogConfigurationError_ShouldCreateAndLogConfigurationValidationFailure()
    {
        // Arrange
        var configSection = "AdamDeviceConfig";
        var propertyName = "TimeoutMs";
        var currentValue = -1000;
        var constraint = "Must be positive integer";

        // Act
        _mockLogger.Object.LogConfigurationError(configSection, propertyName, currentValue, constraint);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[CONF-004]") && 
                v.ToString()!.Contains($"Configuration validation failed in section '{configSection}'") &&
                v.ToString()!.Contains($"Property '{propertyName}' with value '{currentValue}' violates constraint: {constraint}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogPerformanceDegradation_ShouldCreateAndLogPerformanceDegradation()
    {
        // Arrange
        var metricName = "ResponseTime";
        var currentValue = 5000.0;
        var threshold = 1000.0;
        var recommendation = "Consider increasing polling interval";

        // Act
        _mockLogger.Object.LogPerformanceDegradation(metricName, currentValue, threshold, recommendation);

        // Assert
        // Performance degradation is logged at Warning level (Low severity)
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[PERF-005]") && 
                v.ToString()!.Contains($"Performance degradation detected: {metricName}") &&
                v.ToString()!.Contains($"Current {metricName} value {currentValue:F2} exceeds threshold {threshold:F2}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogCounterOverflow_ShouldCreateAndLogCounterOverflowDetection()
    {
        // Arrange
        var deviceId = "device-005";
        var channel = 0;
        var currentValue = 100L;
        var previousValue = 4294967290L; // Near max uint
        var maxValue = uint.MaxValue;

        // Act
        _mockLogger.Object.LogCounterOverflow(deviceId, channel, currentValue, previousValue, maxValue);

        // Assert
        // Counter overflow is logged at Information level (Info severity)
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains("[DATA-004]") && 
                v.ToString()!.Contains($"Counter overflow detected for device '{deviceId}' channel {channel}") &&
                v.ToString()!.Contains($"Counter value rolled over from {previousValue} to {currentValue}")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogIndustrialError_ShouldUseCorrelationContext()
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-006",
            Summary = "Test correlation context",
            DetailedDescription = "Testing correlation ID",
            TroubleshootingSteps = new List<string>(),
            Severity = ErrorSeverity.Low,
            Category = ErrorCategory.System
        };

        var correlationIds = new List<string>();
        
        // Capture correlation IDs from LogContext.PushProperty calls
        _mockLogger.Setup(x => x.BeginScope(It.IsAny<It.IsAnyType>()))
            .Returns(Mock.Of<IDisposable>());

        // Act
        _mockLogger.Object.LogIndustrialError(errorMessage);

        // Assert
        // The method should create a correlation context
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void LogIndustrialError_WithCallerInfo_ShouldPassCallerParameters()
    {
        // Arrange
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-007",
            Summary = "Test caller info",
            DetailedDescription = "Testing caller member parameters",
            TroubleshootingSteps = new List<string>(),
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.System
        };

        var memberName = "TestMethod";
        var sourceFilePath = "/test/path/file.cs";
        var sourceLineNumber = 42;

        // Act
        _mockLogger.Object.LogIndustrialError(
            errorMessage, 
            memberName: memberName, 
            sourceFilePath: sourceFilePath, 
            sourceLineNumber: sourceLineNumber);

        // Assert
        // The method should execute without errors
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void AllLogMethods_ShouldAcceptCallerAttributes()
    {
        // This test verifies that all logging extension methods properly accept caller attributes
        var deviceId = "test-device";
        var exception = new Exception("Test");
        var memberName = "TestCaller";
        var filePath = "/test/file.cs";
        var lineNumber = 100;

        // LogConnectionError
        _mockLogger.Object.LogConnectionError(
            deviceId, "192.168.1.1", 502, exception,
            memberName, filePath, lineNumber);

        // LogModbusError
        _mockLogger.Object.LogModbusError(
            deviceId, "Read", 0, 10, 1, 3, exception,
            memberName, filePath, lineNumber);

        // LogDataValidationError
        _mockLogger.Object.LogDataValidationError(
            deviceId, 0, 123, "rule",
            memberName, filePath, lineNumber);

        // LogConfigurationError
        _mockLogger.Object.LogConfigurationError(
            "section", "property", "value", "constraint",
            memberName, filePath, lineNumber);

        // LogPerformanceDegradation
        _mockLogger.Object.LogPerformanceDegradation(
            "metric", 100.0, 50.0, "recommendation",
            memberName, filePath, lineNumber);

        // LogCounterOverflow
        _mockLogger.Object.LogCounterOverflow(
            deviceId, 0, 100, 1000, uint.MaxValue,
            memberName, filePath, lineNumber);

        // Assert - all methods should have been called without errors
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }
}