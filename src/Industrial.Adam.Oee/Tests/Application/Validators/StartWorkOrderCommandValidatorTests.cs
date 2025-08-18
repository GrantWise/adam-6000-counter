using Industrial.Adam.Oee.Application.Commands;
using Industrial.Adam.Oee.Application.Validators;
using Xunit;

namespace Industrial.Adam.Oee.Tests.Application.Validators;

/// <summary>
/// Unit tests for StartWorkOrderCommandValidator
/// </summary>
public class StartWorkOrderCommandValidatorTests
{
    private readonly StartWorkOrderCommandValidator _validator;

    public StartWorkOrderCommandValidatorTests()
    {
        _validator = new StartWorkOrderCommandValidator();
    }

    [Fact]
    public async Task Validate_ValidCommand_PassesValidation()
    {
        // Arrange
        var command = new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = 100,
            UnitOfMeasure = "pieces",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(8),
            LineId = "DEVICE-001"
        };

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("", "Work order ID is required")]
    [InlineData("   ", "Work order ID is required")]
    [InlineData("WO-001-WITH-A-VERY-LONG-NAME-THAT-EXCEEDS-FIFTY-CHARACTERS", "Work order ID cannot exceed 50 characters")]
    [InlineData("WO@001", "Work order ID can only contain alphanumeric characters, underscores, and hyphens")]
    public async Task Validate_InvalidWorkOrderId_FailsValidation(string workOrderId, string expectedError)
    {
        // Arrange
        var command = CreateValidCommand();
        command.WorkOrderId = workOrderId;

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedError));
    }

    [Theory]
    [InlineData(0, "Planned quantity must be greater than zero")]
    [InlineData(-10, "Planned quantity must be greater than zero")]
    [InlineData(1000001, "Planned quantity cannot exceed 1,000,000")]
    public async Task Validate_InvalidPlannedQuantity_FailsValidation(decimal plannedQuantity, string expectedError)
    {
        // Arrange
        var command = CreateValidCommand();
        command.PlannedQuantity = plannedQuantity;

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedError));
    }

    [Fact]
    public async Task Validate_EndTimeBeforeStartTime_FailsValidation()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ScheduledStartTime = DateTime.UtcNow;
        command.ScheduledEndTime = DateTime.UtcNow.AddHours(-1); // End before start

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Scheduled end time must be after scheduled start time"));
    }

    [Fact]
    public async Task Validate_WorkOrderDurationExceeds24Hours_FailsValidation()
    {
        // Arrange
        var command = CreateValidCommand();
        command.ScheduledStartTime = DateTime.UtcNow;
        command.ScheduledEndTime = DateTime.UtcNow.AddHours(25); // 25 hours duration

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Work order duration cannot exceed 24 hours"));
    }

    [Theory]
    [InlineData("", "Line ID is required")]
    [InlineData("DEVICE-WITH-A-VERY-LONG-NAME", "Line ID cannot exceed 20 characters")]
    [InlineData("DEVICE@001", "Line ID can only contain alphanumeric characters, underscores, and hyphens")]
    public async Task Validate_InvalidLineId_FailsValidation(string deviceId, string expectedError)
    {
        // Arrange
        var command = CreateValidCommand();
        command.LineId = deviceId;

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedError));
    }

    [Theory]
    [InlineData("", "Work order description is required")]
    [InlineData("A very long description that exceeds the maximum allowed length of 200 characters. This description is intentionally made very long to test the validation rule that limits the description to 200 characters maximum length.", "Work order description cannot exceed 200 characters")]
    public async Task Validate_InvalidDescription_FailsValidation(string description, string expectedError)
    {
        // Arrange
        var command = CreateValidCommand();
        command.WorkOrderDescription = description;

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedError));
    }

    [Fact]
    public async Task Validate_ValidOperatorId_PassesValidation()
    {
        // Arrange
        var command = CreateValidCommand();
        command.OperatorId = "OP-001";

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_TooLongOperatorId_FailsValidation()
    {
        // Arrange
        var command = CreateValidCommand();
        command.OperatorId = "OPERATOR-WITH-A-VERY-LONG-NAME-THAT-EXCEEDS-FIFTY-CHARACTERS";

        // Act
        var result = await _validator.ValidateAsync(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Operator ID cannot exceed 50 characters"));
    }

    /// <summary>
    /// Create a valid command for testing
    /// </summary>
    /// <returns>Valid StartWorkOrderCommand</returns>
    private static StartWorkOrderCommand CreateValidCommand()
    {
        return new StartWorkOrderCommand
        {
            WorkOrderId = "WO-001",
            WorkOrderDescription = "Test Work Order",
            ProductId = "PROD-001",
            ProductDescription = "Test Product",
            PlannedQuantity = 100,
            UnitOfMeasure = "pieces",
            ScheduledStartTime = DateTime.UtcNow,
            ScheduledEndTime = DateTime.UtcNow.AddHours(8),
            LineId = "DEVICE-001"
        };
    }
}
