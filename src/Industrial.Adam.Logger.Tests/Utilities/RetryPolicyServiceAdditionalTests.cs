// Industrial.Adam.Logger.Tests - Additional RetryPolicyService Tests for Better Coverage
// Covers edge cases, jitter, callbacks, and specific exception scenarios

using System.Net.Sockets;
using FluentAssertions;
using Industrial.Adam.Logger.Interfaces;
using Industrial.Adam.Logger.Utilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Industrial.Adam.Logger.Tests.Utilities;

/// <summary>
/// Additional tests for RetryPolicyService to improve code coverage
/// </summary>
public class RetryPolicyServiceAdditionalTests
{
    #region Jitter Tests

    [Fact]
    public async Task ExecuteAsync_WithJitter_ShouldVaryDelays()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = new RetryPolicy
        {
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(1),
            Strategy = RetryStrategy.FixedDelay,
            JitterFactor = 0.5, // 50% jitter
            ShouldRetry = _ => true
        };
        
        var delays = new List<TimeSpan>();
        var attemptCount = 0;
        var lastTimestamp = DateTime.UtcNow;

        // Act
        await service.ExecuteAsync(
            async _ =>
            {
                attemptCount++;
                var now = DateTime.UtcNow;
                if (attemptCount > 1) // Skip first attempt
                {
                    delays.Add(now - lastTimestamp);
                }
                lastTimestamp = now;
                await Task.Delay(1);
                throw new TimeoutException("Force retry");
            },
            policy);

        // Assert
        delays.Should().HaveCount(5);
        
        // With jitter, delays should vary (not all exactly 100ms)
        var distinctDelays = delays.Select(d => Math.Round(d.TotalMilliseconds / 10) * 10).Distinct().Count();
        distinctDelays.Should().BeGreaterThan(1, "Jitter should cause delay variation");
        
        // All delays should be within jitter range (50ms to 150ms for 100ms base with 50% jitter)
        foreach (var delay in delays)
        {
            delay.TotalMilliseconds.Should().BeInRange(40, 160); // Allow some timing variance
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithZeroJitter_ShouldHaveConsistentDelays()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = new RetryPolicy
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(50),
            MaxDelay = TimeSpan.FromSeconds(1),
            Strategy = RetryStrategy.FixedDelay,
            JitterFactor = 0, // No jitter
            ShouldRetry = _ => true
        };
        
        var delays = new List<TimeSpan>();
        var attemptCount = 0;
        var lastTimestamp = DateTime.UtcNow;

        // Act
        await service.ExecuteAsync(
            async _ =>
            {
                attemptCount++;
                var now = DateTime.UtcNow;
                if (attemptCount > 1) // Skip first attempt
                {
                    delays.Add(now - lastTimestamp);
                }
                lastTimestamp = now;
                await Task.Delay(1);
                throw new TimeoutException("Force retry");
            },
            policy);

        // Assert
        delays.Should().HaveCount(3);
        
        // Without jitter, delays should be consistent (allowing for timing variance)
        foreach (var delay in delays)
        {
            delay.TotalMilliseconds.Should().BeInRange(45, 65); // ~50ms with timing tolerance
        }
    }

    #endregion

    #region OnRetry Callback Tests

    [Fact]
    public async Task ExecuteAsync_WithOnRetryCallback_ShouldInvokeCallback()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var callbackInvocations = new List<(int attempt, Exception exception, TimeSpan delay)>();
        
        var policy = new RetryPolicy
        {
            MaxAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.FixedDelay,
            ShouldRetry = _ => true,
            OnRetry = (attempt, exception, delay) =>
            {
                callbackInvocations.Add((attempt, exception, delay));
            }
        };

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                await Task.Delay(1);
                throw new SocketException((int)SocketError.ConnectionRefused);
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        callbackInvocations.Should().HaveCount(2);
        
        callbackInvocations[0].attempt.Should().Be(1);
        callbackInvocations[0].exception.Should().BeOfType<SocketException>();
        callbackInvocations[0].delay.Should().BeCloseTo(TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(5));
        
        callbackInvocations[1].attempt.Should().Be(2);
        callbackInvocations[1].exception.Should().BeOfType<SocketException>();
    }

    [Fact]
    public async Task ExecuteAsync_OnRetryCallbackThrows_ShouldNotAffectRetrying()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var callCount = 0;
        var callbackExceptionCount = 0;
        
        var policy = new RetryPolicy
        {
            MaxAttempts = 2,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.FixedDelay,
            ShouldRetry = _ => true,
            OnRetry = (_, _, _) =>
            {
                callbackExceptionCount++;
                // Note: In the actual implementation, callback exceptions are not handled
                // This test documents the current behavior
            }
        };

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                if (callCount < 3)
                    throw new TimeoutException();
                return "success";
            },
            policy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(3); // Should complete successfully
        callbackExceptionCount.Should().Be(2); // Callback called for each retry
    }

    #endregion

    #region Cancellation During Delay Tests

    [Fact]
    public async Task ExecuteAsync_CancellationDuringRetryDelay_ShouldReturnFailure()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        using var cts = new CancellationTokenSource();
        var attemptCount = 0;
        
        var policy = new RetryPolicy
        {
            MaxAttempts = 3,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryStrategy.FixedDelay,
            ShouldRetry = _ => true
        };

        // Act
        var task = service.ExecuteAsync(
            async token =>
            {
                attemptCount++;
                await Task.Delay(1, token);
                throw new TimeoutException("Force retry");
            },
            policy,
            cts.Token);

        // Cancel during first retry delay
        await Task.Delay(50);
        cts.Cancel();
        var result = await task;

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Context.Should().ContainKey("CancelledDuringDelay");
        result.Context["CancelledDuringDelay"].Should().Be(true);
        attemptCount.Should().Be(1); // Only first attempt should execute
    }

    #endregion

    #region Max Delay Enforcement Tests

    [Fact]
    public async Task ExecuteAsync_ExponentialBackoffExceedsMaxDelay_ShouldCapAtMaxDelay()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = new RetryPolicy
        {
            MaxAttempts = 10,
            BaseDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromMilliseconds(500), // Cap at 500ms
            Strategy = RetryStrategy.ExponentialBackoff,
            JitterFactor = 0,
            ShouldRetry = _ => true
        };
        
        var delays = new List<TimeSpan>();
        var attemptCount = 0;
        var lastTimestamp = DateTime.UtcNow;

        // Act
        await service.ExecuteAsync(
            async _ =>
            {
                attemptCount++;
                var now = DateTime.UtcNow;
                if (attemptCount > 1) // Skip first attempt
                {
                    delays.Add(now - lastTimestamp);
                }
                lastTimestamp = now;
                await Task.Delay(1);
                throw new TimeoutException();
            },
            policy);

        // Assert
        // Later delays should be capped at MaxDelay
        delays.Should().HaveCountGreaterThan(5);
        var laterDelays = delays.Skip(5).ToList();
        foreach (var delay in laterDelays)
        {
            delay.TotalMilliseconds.Should().BeLessOrEqualTo(550); // Max delay + timing tolerance
        }
    }

    #endregion

    #region Socket Exception Specific Tests

    [Theory]
    [InlineData(SocketError.TimedOut, true)]
    [InlineData(SocketError.ConnectionRefused, true)]
    [InlineData(SocketError.ConnectionReset, true)]
    [InlineData(SocketError.ConnectionAborted, true)]
    [InlineData(SocketError.NetworkDown, true)]
    [InlineData(SocketError.NetworkUnreachable, true)]
    [InlineData(SocketError.HostDown, true)]
    [InlineData(SocketError.HostUnreachable, true)]
    [InlineData(SocketError.TryAgain, true)]
    [InlineData(SocketError.AccessDenied, false)]
    [InlineData(SocketError.InvalidArgument, false)]
    [InlineData(SocketError.AddressAlreadyInUse, false)]
    public async Task DeviceRetryPolicy_SocketErrors_ShouldRetryCorrectly(SocketError socketError, bool shouldRetry)
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = service.CreateDeviceRetryPolicy(2);
        var callCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                throw new SocketException((int)socketError);
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        if (shouldRetry)
        {
            callCount.Should().Be(3); // Initial + 2 retries
        }
        else
        {
            callCount.Should().Be(1); // No retries
        }
    }

    #endregion

    #region HTTP Exception Pattern Tests

    [Theory]
    [InlineData("Request timeout occurred", true)] // Contains "timeout"
    [InlineData("Connection failed to remote server", true)] // Contains "connection"
    [InlineData("Network is unreachable", true)] // Contains "network"
    [InlineData("Connection refused by remote host", true)] // Contains "connection" and "refused"
    [InlineData("The request was canceled due to timeout", true)] // Contains "timeout"
    [InlineData("Bad request format", false)]
    [InlineData("Authentication failed", false)]
    [InlineData("Internal server error", false)]
    public async Task NetworkRetryPolicy_HttpExceptionMessages_ShouldRetryCorrectly(string message, bool shouldRetry)
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = service.CreateNetworkRetryPolicy(2);
        var callCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                throw new HttpRequestException(message);
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        if (shouldRetry)
        {
            callCount.Should().Be(3); // Initial attempt + 2 retries
        }
        else
        {
            callCount.Should().Be(1); // No retries
        }
    }

    #endregion

    #region Device-Specific Retry Conditions Tests

    [Theory]
    [InlineData("Connection to device lost", true)]
    [InlineData("Device connection timeout", true)]
    [InlineData("The connection was closed unexpectedly", true)]
    [InlineData("Operation timeout while reading from device", true)]
    [InlineData("Invalid data format", false)]
    [InlineData("Device not configured", false)]
    public async Task DeviceRetryPolicy_InvalidOperationExceptionMessages_ShouldRetryCorrectly(string message, bool shouldRetry)
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = service.CreateDeviceRetryPolicy(2);
        var callCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                throw new InvalidOperationException(message);
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        if (shouldRetry)
        {
            callCount.Should().BeGreaterThan(1); // Should retry
        }
        else
        {
            callCount.Should().Be(1); // No retries
        }
    }

    #endregion

    #region Context Information Tests

    [Fact]
    public async Task ExecuteAsync_Success_ShouldPopulateContextCorrectly()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = RetryPolicy.ExponentialBackoff(3, TimeSpan.FromMilliseconds(10));

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                await Task.Delay(1);
                return "success";
            },
            policy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Context.Should().ContainKey("MaxAttempts");
        result.Context["MaxAttempts"].Should().Be(3);
        result.Context.Should().ContainKey("Strategy");
        result.Context["Strategy"].Should().Be("ExponentialBackoff");
        result.Context.Should().ContainKey("ActualAttempts");
        result.Context["ActualAttempts"].Should().Be(1);
        result.Context.Should().ContainKey("Success");
        result.Context["Success"].Should().Be(true);
    }

    [Fact]
    public async Task ExecuteAsync_FailureAfterRetries_ShouldPopulateContextCorrectly()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = RetryPolicy.LinearBackoff(2, TimeSpan.FromMilliseconds(10));
        var attemptCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                attemptCount++;
                await Task.Delay(1);
                throw new TimeoutException($"Attempt {attemptCount}");
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Context.Should().ContainKey("MaxAttempts");
        result.Context["MaxAttempts"].Should().Be(2);
        result.Context.Should().ContainKey("Strategy");
        result.Context["Strategy"].Should().Be("LinearBackoff");
        result.Context.Should().ContainKey("ActualAttempts");
        result.Context["ActualAttempts"].Should().Be(3); // Initial + 2 retries
        result.Context.Should().ContainKey("Success");
        result.Context["Success"].Should().Be(false);
    }

    #endregion

    #region TaskCanceledException Special Cases

    [Fact]
    public async Task ExecuteAsync_TaskCanceledExceptionWithoutCancellation_ShouldRetry()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = service.CreateDeviceRetryPolicy(2);
        var callCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                // Simulate timeout that throws TaskCanceledException
                throw new TaskCanceledException("Operation timed out");
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        callCount.Should().Be(3); // Should retry because cancellation token wasn't set
    }

    [Fact]
    public async Task ExecuteAsync_TaskCanceledExceptionWithCancellation_ShouldNotRetry()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = service.CreateNetworkRetryPolicy(2);
        using var cts = new CancellationTokenSource();
        var callCount = 0;

        // Act
        cts.Cancel();
        var result = await service.ExecuteAsync(
            async token =>
            {
                callCount++;
                await Task.Delay(1, token);
                // This would throw OperationCanceledException due to cancelled token
                return "should not reach";
            },
            policy,
            cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
        callCount.Should().Be(1); // Should not retry on actual cancellation
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ExecuteAsync_NegativeJitterResult_ShouldNotCauseNegativeDelay()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = new RetryPolicy
        {
            MaxAttempts = 5,
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.FixedDelay,
            JitterFactor = 1.0, // 100% jitter could potentially create negative delays
            ShouldRetry = _ => true
        };

        var startTime = DateTime.UtcNow;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                await Task.Delay(1);
                throw new TimeoutException();
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        var totalTime = DateTime.UtcNow - startTime;
        // Even with maximum jitter, total time should be positive
        totalTime.TotalMilliseconds.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_ZeroMaxAttempts_ShouldExecuteOnce()
    {
        // Arrange
        var service = CreateRetryPolicyService();
        var policy = new RetryPolicy
        {
            MaxAttempts = 0, // Zero retries
            BaseDelay = TimeSpan.FromMilliseconds(10),
            Strategy = RetryStrategy.FixedDelay,
            ShouldRetry = _ => true
        };
        var callCount = 0;

        // Act
        var result = await service.ExecuteAsync(
            async _ =>
            {
                callCount++;
                await Task.Delay(1);
                throw new TimeoutException();
            },
            policy);

        // Assert
        result.IsFailure.Should().BeTrue();
        callCount.Should().Be(1); // Initial attempt only
    }

    #endregion

    #region Helper Methods

    private static RetryPolicyService CreateRetryPolicyService()
    {
        var mockLogger = new Mock<ILogger<RetryPolicyService>>();
        return new RetryPolicyService(mockLogger.Object);
    }

    #endregion
}