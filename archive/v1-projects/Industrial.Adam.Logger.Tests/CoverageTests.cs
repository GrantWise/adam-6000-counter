// Industrial.Adam.Logger.Tests - Coverage Tests
// Minimal tests that exercise production code to ensure coverage measurement works

using FluentAssertions;
using Industrial.Adam.Logger.Configuration;
using Industrial.Adam.Logger.Models;
using Industrial.Adam.Logger.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Industrial.Adam.Logger.Tests;

/// <summary>
/// Minimal tests that exercise real production code to verify coverage collection
/// </summary>
public class CoverageTests
{
    [Fact]
    public void RealValidator_ShouldExecuteProductionCode()
    {
        // Arrange - Use real implementation
        var validator = new DefaultDataValidator();
        var channelConfig = new ChannelConfig
        {
            Name = "TestChannel",
            MinValue = 0,
            MaxValue = 1000
        };

        var reading = new AdamDataReading
        {
            DeviceId = "TEST-001",
            Channel = 1,
            RawValue = 500,
            Timestamp = DateTimeOffset.UtcNow,
            Quality = DataQuality.Good
        };

        // Act - Exercise real validation logic
        var result = validator.ValidateReading(reading, channelConfig);

        // Assert - Verify production code executed
        result.Should().Be(DataQuality.Good);
    }

    [Fact]
    public void RealTransformer_ShouldExecuteProductionCode()
    {
        // Arrange - Use real implementation
        var transformer = new DefaultDataTransformer();
        var channelConfig = new ChannelConfig
        {
            ScaleFactor = 0.5,
            Offset = 100.0
        };

        // Act - Exercise real transformation logic
        var result = transformer.TransformValue(200, channelConfig);

        // Assert - Verify production code executed (200 * 0.5 + 100 = 200)
        result.Should().Be(200.0);
    }

    [Fact]
    public void RealProcessor_ShouldExecuteProductionCode()
    {
        // Arrange - Use real implementations
        var validator = new DefaultDataValidator();
        var transformer = new DefaultDataTransformer();
        var logger = NullLogger<DefaultDataProcessor>.Instance;

        var processor = new DefaultDataProcessor(validator, transformer, logger);

        var channelConfig = new ChannelConfig
        {
            ChannelNumber = 1,
            Name = "TestChannel",
            StartRegister = 0,
            RegisterCount = 1,
            ScaleFactor = 1.0,
            Offset = 0.0,
            Unit = "counts"
        };

        var registers = new ushort[] { 1500 };
        var timestamp = DateTimeOffset.UtcNow;

        // Act - Exercise real processing logic
        var result = processor.ProcessRawData("TEST-001", channelConfig, registers, timestamp, TimeSpan.Zero);

        // Assert - Verify production code executed
        result.Should().NotBeNull();
        result.DeviceId.Should().Be("TEST-001");
        result.RawValue.Should().Be(1500);
    }
}
