// Industrial.Adam.Logger.Tests - IndustrialErrorService Unit Tests
// Tests for the comprehensive industrial error handling service

using FluentAssertions;
using Industrial.Adam.Logger.ErrorHandling;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Logging;
using Industrial.Adam.Logger.Tests.TestHelpers;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.ErrorHandling;

/// <summary>
/// Unit tests for IndustrialErrorService focusing on error message creation and logging integration
/// </summary>
public class IndustrialErrorServiceTests : IDisposable
{
    private readonly Mock<ILogger<IndustrialErrorService>> _mockLogger;
    private readonly List<IndustrialErrorService> _servicesToDispose;

    public IndustrialErrorServiceTests()
    {
        _mockLogger = new Mock<ILogger<IndustrialErrorService>>();
        _servicesToDispose = new List<IndustrialErrorService>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ValidLogger_ShouldCreateInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action action = () => new IndustrialErrorService(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CreateAndLogError Tests (Exception overload)

    [Fact]
    public void CreateAndLogError_ValidException_ShouldCreateIndustrialErrorMessage()
    {
        // Arrange
        var service = CreateService();
        var exception = new InvalidOperationException("Test exception");
        const string errorCode = "TEST-001";
        const string summary = "Test error summary";

        // Act
        var result = service.CreateAndLogError(exception, errorCode, summary);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCode.Should().Be(errorCode);
        result.Summary.Should().Be(summary);
        result.DetailedDescription.Should().Be(exception.Message);
        result.OriginalException.Should().Be(exception);
        result.Severity.Should().Be(ErrorSeverity.High);
        result.Category.Should().Be(ErrorCategory.System);
        result.TroubleshootingSteps.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateAndLogError_WithAdditionalContext_ShouldIncludeContextInResult()
    {
        // Arrange
        var service = CreateService();
        var exception = new ArgumentException("Test exception");
        const string errorCode = "TEST-002";
        const string summary = "Test error with context";
        var additionalContext = new Dictionary<string, object>
        {
            ["DeviceId"] = "DEVICE-123",
            ["Channel"] = 1,
            ["CustomProperty"] = "CustomValue"
        };

        // Act
        var result = service.CreateAndLogError(exception, errorCode, summary, additionalContext);

        // Assert
        result.Should().NotBeNull();
        result.Context.Should().ContainKey("DeviceId");
        result.Context.Should().ContainKey("Channel");
        result.Context.Should().ContainKey("CustomProperty");
        result.Context["DeviceId"].Should().Be("DEVICE-123");
        result.Context["Channel"].Should().Be(1);
        result.Context["CustomProperty"].Should().Be("CustomValue");

        // Should also contain original exception context
        result.Context.Should().ContainKey("ExceptionType");
        result.Context.Should().ContainKey("StackTrace");
        result.Context.Should().ContainKey("InnerException");
    }

    [Fact]
    public void CreateAndLogError_WithNullContext_ShouldNotThrow()
    {
        // Arrange
        var service = CreateService();
        var exception = new InvalidOperationException("Test exception");
        const string errorCode = "TEST-003";
        const string summary = "Test error without context";

        // Act
        var result = service.CreateAndLogError(exception, errorCode, summary, null);

        // Assert
        result.Should().NotBeNull();
        result.ErrorCode.Should().Be(errorCode);
        result.Summary.Should().Be(summary);
    }

    [Fact]
    public void CreateAndLogError_ShouldCallLogError()
    {
        // Arrange
        var service = CreateService();
        var exception = new InvalidOperationException("Test exception");
        const string errorCode = "TEST-004";
        const string summary = "Test error logging";

        // Act
        service.CreateAndLogError(exception, errorCode, summary);

        // Assert
        // Verify logging was called by checking mock interactions
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region CreateAndLogError Tests (IndustrialErrorMessage overload)

    [Fact]
    public void CreateAndLogError_ValidIndustrialErrorMessage_ShouldReturnSameInstance()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-005",
            Summary = "Test error message",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Configuration
        };

        // Act
        var result = service.CreateAndLogError(errorMessage);

        // Assert
        result.Should().BeSameAs(errorMessage);
    }

    [Fact]
    public void CreateAndLogError_IndustrialErrorMessage_ShouldCallLogError()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-006",
            Summary = "Test error message",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Low,
            Category = ErrorCategory.Data
        };

        // Act
        service.CreateAndLogError(errorMessage);

        // Assert
        // Verify logging was called
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region CreateFailureResult Tests

    [Fact]
    public void CreateFailureResult_ValidIndustrialErrorMessage_ShouldReturnFailedOperationResult()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-007",
            Summary = "Test failure result",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.Communication
        };

        // Act
        var result = service.CreateFailureResult(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Context.Should().ContainKey("ErrorCode");
        result.Context.Should().ContainKey("ErrorSeverity");
        result.Context.Should().ContainKey("ErrorCategory");
        result.Context.Should().ContainKey("TroubleshootingSteps");
        result.Context["ErrorCode"].Should().Be("TEST-007");
        result.Context["ErrorSeverity"].Should().Be("High");
        result.Context["ErrorCategory"].Should().Be("Communication");
    }

    [Fact]
    public void CreateFailureResult_WithOriginalException_ShouldUseOriginalException()
    {
        // Arrange
        var service = CreateService();
        var originalException = new TimeoutException("Connection timeout");
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-008",
            Summary = "Test failure with original exception",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Critical,
            Category = ErrorCategory.Connection,
            OriginalException = originalException
        };

        // Act
        var result = service.CreateFailureResult(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(originalException);
    }

    [Fact]
    public void CreateFailureResult_WithoutOriginalException_ShouldCreateInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-009",
            Summary = "Test failure without original exception",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.Data
        };

        // Act
        var result = service.CreateFailureResult(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<InvalidOperationException>();
        result.Error!.Message.Should().Be("Test detailed description");
    }

    #endregion

    #region CreateFailureResult<T> Tests

    [Fact]
    public void CreateFailureResult_Generic_ValidIndustrialErrorMessage_ShouldReturnFailedOperationResult()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-010",
            Summary = "Test generic failure result",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.Performance
        };

        // Act
        var result = service.CreateFailureResult<string>(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Context.Should().ContainKey("ErrorCode");
        result.Context.Should().ContainKey("ErrorSeverity");
        result.Context.Should().ContainKey("ErrorCategory");
        result.Context.Should().ContainKey("TroubleshootingSteps");
        result.Context["ErrorCode"].Should().Be("TEST-010");
        result.Context["ErrorSeverity"].Should().Be("High");
        result.Context["ErrorCategory"].Should().Be("Performance");
    }

    [Fact]
    public void CreateFailureResult_Generic_WithOriginalException_ShouldUseOriginalException()
    {
        // Arrange
        var service = CreateService();
        var originalException = new ArgumentException("Invalid argument");
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-011",
            Summary = "Test generic failure with original exception",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Critical,
            Category = ErrorCategory.Hardware,
            OriginalException = originalException
        };

        // Act
        var result = service.CreateFailureResult<int>(errorMessage);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(originalException);
    }

    #endregion

    #region CreateFailureResultFromException Tests

    [Fact]
    public void CreateFailureResultFromException_ValidException_ShouldReturnFailedOperationResult()
    {
        // Arrange
        var service = CreateService();
        var exception = new InvalidOperationException("Test exception");
        const string errorCode = "TEST-012";
        const string summary = "Test failure from exception";

        // Act
        var result = service.CreateFailureResultFromException(exception, errorCode, summary);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Context.Should().ContainKey("ErrorCode");
        result.Context.Should().ContainKey("ErrorSeverity");
        result.Context.Should().ContainKey("ErrorCategory");
        result.Context.Should().ContainKey("TroubleshootingSteps");
        result.Context["ErrorCode"].Should().Be("TEST-012");
    }

    [Fact]
    public void CreateFailureResultFromException_WithContext_ShouldIncludeContextInResult()
    {
        // Arrange
        var service = CreateService();
        var exception = new ArgumentException("Test exception");
        const string errorCode = "TEST-013";
        const string summary = "Test failure with context";
        var context = new Dictionary<string, object>
        {
            ["DeviceId"] = "DEVICE-456",
            ["Channel"] = 2
        };

        // Act
        var result = service.CreateFailureResultFromException(exception, errorCode, summary, context);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Context.Should().ContainKey("DeviceId");
        result.Context.Should().ContainKey("Channel");
        result.Context["DeviceId"].Should().Be("DEVICE-456");
        result.Context["Channel"].Should().Be(2);
    }

    #endregion

    #region CreateFailureResultFromException<T> Tests

    [Fact]
    public void CreateFailureResultFromException_Generic_ValidException_ShouldReturnFailedOperationResult()
    {
        // Arrange
        var service = CreateService();
        var exception = new InvalidOperationException("Test exception");
        const string errorCode = "TEST-014";
        const string summary = "Test generic failure from exception";

        // Act
        var result = service.CreateFailureResultFromException<string>(exception, errorCode, summary);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Context.Should().ContainKey("ErrorCode");
        result.Context.Should().ContainKey("ErrorSeverity");
        result.Context.Should().ContainKey("ErrorCategory");
        result.Context.Should().ContainKey("TroubleshootingSteps");
        result.Context["ErrorCode"].Should().Be("TEST-014");
    }

    [Fact]
    public void CreateFailureResultFromException_Generic_WithContext_ShouldIncludeContextInResult()
    {
        // Arrange
        var service = CreateService();
        var exception = new ArgumentException("Test exception");
        const string errorCode = "TEST-015";
        const string summary = "Test generic failure with context";
        var context = new Dictionary<string, object>
        {
            ["DeviceId"] = "DEVICE-789",
            ["Channel"] = 3
        };

        // Act
        var result = service.CreateFailureResultFromException<int>(exception, errorCode, summary, context);

        // Assert
        result.Should().NotBeNull();
        result.IsFailure.Should().BeTrue();
        result.Context.Should().ContainKey("DeviceId");
        result.Context.Should().ContainKey("Channel");
        result.Context["DeviceId"].Should().Be("DEVICE-789");
        result.Context["Channel"].Should().Be(3);
    }

    #endregion

    #region GetErrorMessageTemplate Tests

    [Fact]
    public void GetErrorMessageTemplate_ExistingErrorCode_ShouldReturnTemplate()
    {
        // Arrange
        var service = CreateService();
        const string errorCode = "CONN-001";

        // Act
        var result = service.GetErrorMessageTemplate(errorCode);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Failed to establish connection");
        result.Should().Contain("{0}");
        result.Should().Contain("{1}");
        result.Should().Contain("{2}");
    }

    [Fact]
    public void GetErrorMessageTemplate_NonExistentErrorCode_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();
        const string errorCode = "NONEXISTENT-999";

        // Act
        var result = service.GetErrorMessageTemplate(errorCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetErrorMessageTemplate_AllBuiltInErrorCodes_ShouldReturnTemplates()
    {
        // Arrange
        var service = CreateService();
        var errorCodes = new[]
        {
            "CONN-001", "CONN-002", "COMM-002", "DATA-003", "DATA-004", "CONF-004", "PERF-005"
        };

        // Act & Assert
        foreach (var errorCode in errorCodes)
        {
            var result = service.GetErrorMessageTemplate(errorCode);
            result.Should().NotBeNull($"Error code {errorCode} should have a template");
            result.Should().NotBeEmpty($"Error code {errorCode} template should not be empty");
        }
    }

    #endregion

    #region GetTroubleshootingSteps Tests

    [Fact]
    public void GetTroubleshootingSteps_ExistingErrorCode_ShouldReturnSteps()
    {
        // Arrange
        var service = CreateService();
        const string errorCode = "CONN-001";

        // Act
        var result = service.GetTroubleshootingSteps(errorCode);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().Contain("Verify network connectivity with ping");
        result.Should().Contain("Check firewall settings");
        result.Should().Contain("Verify device power and network cable");
        result.Should().Contain("Check device IP configuration");
    }

    [Fact]
    public void GetTroubleshootingSteps_NonExistentErrorCode_ShouldReturnNull()
    {
        // Arrange
        var service = CreateService();
        const string errorCode = "NONEXISTENT-999";

        // Act
        var result = service.GetTroubleshootingSteps(errorCode);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTroubleshootingSteps_AllBuiltInErrorCodes_ShouldReturnSteps()
    {
        // Arrange
        var service = CreateService();
        var errorCodes = new[]
        {
            "CONN-001", "COMM-002", "DATA-003", "DATA-004", "CONF-004", "PERF-005"
        };

        // Act & Assert
        foreach (var errorCode in errorCodes)
        {
            var result = service.GetTroubleshootingSteps(errorCode);
            result.Should().NotBeNull($"Error code {errorCode} should have troubleshooting steps");
            result.Should().NotBeEmpty($"Error code {errorCode} troubleshooting steps should not be empty");
        }
    }

    #endregion

    #region LogError Tests

    [Fact]
    public void LogError_ValidIndustrialErrorMessage_ShouldLogAtAppropriateLevel()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-016",
            Summary = "Test logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.System
        };

        // Act
        service.LogError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogError_CriticalSeverity_ShouldLogAtCriticalLevel()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-017",
            Summary = "Test critical logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Critical,
            Category = ErrorCategory.System
        };

        // Act
        service.LogError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogError_InfoSeverity_ShouldLogAtInformationLevel()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-018",
            Summary = "Test info logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Info,
            Category = ErrorCategory.System
        };

        // Act
        service.LogError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogError_LowSeverity_ShouldLogAtWarningLevel()
    {
        // Arrange
        var service = CreateService();
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-019",
            Summary = "Test warning logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Low,
            Category = ErrorCategory.System
        };

        // Act
        service.LogError(errorMessage);

        // Assert
        _mockLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void LogError_WithOriginalException_ShouldUseStructuredErrorLogging()
    {
        // Arrange
        var service = CreateService();
        var originalException = new InvalidOperationException("Test exception");
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-020",
            Summary = "Test exception logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.Medium,
            Category = ErrorCategory.System,
            OriginalException = originalException
        };

        // Act
        service.LogError(errorMessage);

        // Assert
        // Verify structured error logging was used
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            originalException,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region LogError with CorrelationId Tests

    [Fact]
    public void LogError_WithCorrelationId_ShouldLogWithCorrelationContext()
    {
        // Arrange
        var service = CreateService();
        var correlationId = "CORRELATION-123";
        var errorMessage = new IndustrialErrorMessage
        {
            ErrorCode = "TEST-021",
            Summary = "Test correlation logging",
            DetailedDescription = "Test detailed description",
            TroubleshootingSteps = new[] { "Step 1", "Step 2" },
            Severity = ErrorSeverity.High,
            Category = ErrorCategory.System
        };

        // Act
        service.LogError(errorMessage, correlationId);

        // Assert
        _mockLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Helper Methods

    private IndustrialErrorService CreateService()
    {
        var service = new IndustrialErrorService(_mockLogger.Object);
        _servicesToDispose.Add(service);
        return service;
    }

    #endregion

    public void Dispose()
    {
        foreach (var service in _servicesToDispose)
        {
            try
            {
                // IndustrialErrorService doesn't implement IDisposable, so just clear the reference
            }
            catch
            {
                // Ignore disposal errors in tests
            }
        }
        _servicesToDispose.Clear();
    }
}
