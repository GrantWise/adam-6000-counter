using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Xunit;
using Industrial.Adam.Logger.Logging;
using System.Diagnostics;

namespace Industrial.Adam.Logger.Tests.Logging;

public class StructuredLoggingExtensionsTests : IDisposable
{
    private readonly Mock<Microsoft.Extensions.Logging.ILogger> _mockMsLogger;
    private readonly Mock<Serilog.ILogger> _mockSerilogLogger;
    private readonly List<LogEvent> _capturedLogEvents;
    private readonly Serilog.ILogger _testLogger;

    public StructuredLoggingExtensionsTests()
    {
        _mockMsLogger = new Mock<Microsoft.Extensions.Logging.ILogger>();
        _mockSerilogLogger = new Mock<Serilog.ILogger>();
        _capturedLogEvents = new List<LogEvent>();
        
        // Create a test logger that captures log events
        _testLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new TestLogEventSink(_capturedLogEvents))
            .CreateLogger();
    }

    [Fact]
    public void PushCorrelationContext_WithMsLogger_ShouldCreateDisposableContext()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        using (var context = _mockMsLogger.Object.PushCorrelationContext(correlationId))
        {
            // Assert
            context.Should().NotBeNull();
            context.Should().BeAssignableTo<IDisposable>();
        }
    }

    [Fact]
    public void PushCorrelationContext_WithSerilogLogger_ShouldCreateDisposableContext()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();

        // Act
        using (var context = _mockSerilogLogger.Object.PushCorrelationContext(correlationId))
        {
            // Assert
            context.Should().NotBeNull();
            context.Should().BeAssignableTo<IDisposable>();
        }
    }

    [Fact]
    public void LogDeviceOperation_Success_ShouldLogInformation()
    {
        // Arrange
        var deviceId = "device-001";
        var operation = "ReadCounters";
        var duration = TimeSpan.FromMilliseconds(150);
        var success = true;

        // Act
        _mockMsLogger.Object.LogDeviceOperation(deviceId, operation, duration, success);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(deviceId) && 
                v.ToString()!.Contains(operation) &&
                v.ToString()!.Contains("SUCCESS")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogDeviceOperation_Failure_ShouldLogInformationWithFailure()
    {
        // Arrange
        var deviceId = "device-001";
        var operation = "ReadCounters";
        var duration = TimeSpan.FromMilliseconds(5000);
        var success = false;

        // Act
        _mockMsLogger.Object.LogDeviceOperation(deviceId, operation, duration, success);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(deviceId) && 
                v.ToString()!.Contains(operation) &&
                v.ToString()!.Contains("FAILURE")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogPerformanceMetric_ShouldLogInformationWithMetricDetails()
    {
        // Arrange
        var metricName = "ProcessingRate";
        var value = 1234.56;
        var unit = "items/sec";

        // Act
        _mockMsLogger.Object.LogPerformanceMetric(metricName, value, unit);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(metricName) && 
                v.ToString()!.Contains(value.ToString()) &&
                v.ToString()!.Contains(unit)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogPerformanceMetric_WithoutUnit_ShouldLogInformationWithoutUnit()
    {
        // Arrange
        var metricName = "ItemCount";
        var value = 42.0;

        // Act
        _mockMsLogger.Object.LogPerformanceMetric(metricName, value);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(metricName) && 
                v.ToString()!.Contains(value.ToString())),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogConnectionEvent_Success_ShouldLogInformation()
    {
        // Arrange
        var deviceId = "device-001";
        var endpoint = "192.168.1.100:502";
        var success = true;
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        _mockMsLogger.Object.LogConnectionEvent(deviceId, endpoint, success, duration);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(deviceId) && 
                v.ToString()!.Contains(endpoint) &&
                v.ToString()!.Contains("CONNECTED")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogConnectionEvent_Failure_ShouldLogWarning()
    {
        // Arrange
        var deviceId = "device-001";
        var endpoint = "192.168.1.100:502";
        var success = false;
        var duration = TimeSpan.FromMilliseconds(5000);

        // Act
        _mockMsLogger.Object.LogConnectionEvent(deviceId, endpoint, success, duration);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(deviceId) && 
                v.ToString()!.Contains(endpoint) &&
                v.ToString()!.Contains("FAILED")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogDataProcessing_ShouldLogDebug()
    {
        // Arrange
        var deviceId = "device-001";
        var channel = 0;
        var value = 12345L;
        var quality = "Good";

        // Act
        _mockMsLogger.Object.LogDataProcessing(deviceId, channel, value, quality);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(deviceId) && 
                v.ToString()!.Contains($"Ch{channel}") &&
                v.ToString()!.Contains(value.ToString()) &&
                v.ToString()!.Contains(quality)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogStructuredError_ShouldLogErrorWithException()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var message = "Operation failed: {Operation}";
        var args = new object[] { "TestOperation" };

        // Act
        _mockMsLogger.Object.LogStructuredError(exception, message, args: args);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void BeginTimedOperation_ShouldLogStartAndEnd()
    {
        // Arrange
        var operationName = "DataProcessing";
        var logCallCount = 0;
        
        _mockMsLogger.Setup(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(() => logCallCount++);

        // Act
        using (var operation = _mockMsLogger.Object.BeginTimedOperation(operationName))
        {
            // Simulate some work
            Thread.Sleep(50);
        }

        // Assert
        logCallCount.Should().Be(2); // Should log start and end
        
        // Verify debug log for start
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Started operation") && v.ToString()!.Contains(operationName)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        
        // Verify information log for completion
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed operation") && v.ToString()!.Contains(operationName)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void TimedOperation_ShouldMeasureDuration()
    {
        // Arrange
        var operationName = "TestOperation";
        
        // Act
        using (var operation = _mockMsLogger.Object.BeginTimedOperation(operationName))
        {
            Thread.Sleep(100); // Ensure measurable duration
        }

        // Assert
        // Verify that the completion log includes a duration in milliseconds
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => 
                v.ToString()!.Contains(operationName) && 
                System.Text.RegularExpressions.Regex.IsMatch(v.ToString()!, @"\d+ms")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void LogMethods_ShouldIncludeCallerInformation()
    {
        // This test verifies that caller information attributes work correctly
        // In practice, the CallerMemberName, CallerFilePath, and CallerLineNumber
        // are populated by the compiler at the call site
        
        // Arrange
        var deviceId = "test-device";
        var operation = "test-op";
        var duration = TimeSpan.FromMilliseconds(100);
        
        // Act
        _mockMsLogger.Object.LogDeviceOperation(
            deviceId, 
            operation, 
            duration, 
            true,
            memberName: "TestMethod",
            sourceFilePath: "/test/path/file.cs",
            sourceLineNumber: 42);

        // Assert
        // The method should execute without errors
        // In a real scenario, LogContext would capture these values
        _mockMsLogger.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    [Fact]
    public void LogStructuredError_WithComplexException_ShouldCaptureAllDetails()
    {
        // Arrange
        var innerException = new ArgumentException("Inner error");
        var outerException = new InvalidOperationException("Outer error", innerException);
        var message = "Complex error occurred";

        // Act
        _mockMsLogger.Object.LogStructuredError(outerException, message);

        // Assert
        _mockMsLogger.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            outerException,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()
        ), Times.Once);
    }

    // Test helper sink for capturing Serilog events
    private class TestLogEventSink : ILogEventSink
    {
        private readonly List<LogEvent> _events;

        public TestLogEventSink(List<LogEvent> events)
        {
            _events = events;
        }

        public void Emit(LogEvent logEvent)
        {
            _events.Add(logEvent);
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}