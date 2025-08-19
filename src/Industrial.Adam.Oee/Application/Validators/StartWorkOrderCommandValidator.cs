using FluentValidation;
using Industrial.Adam.Oee.Application.Commands;

namespace Industrial.Adam.Oee.Application.Validators;

/// <summary>
/// Validator for StartWorkOrderCommand
/// </summary>
public class StartWorkOrderCommandValidator : AbstractValidator<StartWorkOrderCommand>
{
    /// <summary>
    /// Initializes a new instance of the StartWorkOrderCommandValidator class
    /// </summary>
    public StartWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithMessage("Work order ID is required")
            .MaximumLength(50)
            .WithMessage("Work order ID cannot exceed 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Work order ID can only contain alphanumeric characters, underscores, and hyphens");

        RuleFor(x => x.WorkOrderDescription)
            .NotEmpty()
            .WithMessage("Work order description is required")
            .MaximumLength(200)
            .WithMessage("Work order description cannot exceed 200 characters");

        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("Product ID is required")
            .MaximumLength(50)
            .WithMessage("Product ID cannot exceed 50 characters");

        RuleFor(x => x.ProductDescription)
            .NotEmpty()
            .WithMessage("Product description is required")
            .MaximumLength(200)
            .WithMessage("Product description cannot exceed 200 characters");

        RuleFor(x => x.PlannedQuantity)
            .GreaterThan(0)
            .WithMessage("Planned quantity must be greater than zero")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Planned quantity cannot exceed 1,000,000");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty()
            .WithMessage("Unit of measure is required")
            .MaximumLength(20)
            .WithMessage("Unit of measure cannot exceed 20 characters");

        RuleFor(x => x.LineId)
            .NotEmpty()
            .WithMessage("Line ID is required")
            .MaximumLength(20)
            .WithMessage("Line ID cannot exceed 20 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Line ID can only contain alphanumeric characters, underscores, and hyphens");

        RuleFor(x => x.ScheduledStartTime)
            .NotEmpty()
            .WithMessage("Scheduled start time is required")
            .Must(BeValidDateTime)
            .WithMessage("Scheduled start time must be a valid date and time");

        RuleFor(x => x.ScheduledEndTime)
            .NotEmpty()
            .WithMessage("Scheduled end time is required")
            .Must(BeValidDateTime)
            .WithMessage("Scheduled end time must be a valid date and time")
            .GreaterThan(x => x.ScheduledStartTime)
            .WithMessage("Scheduled end time must be after scheduled start time");

        RuleFor(x => x.OperatorId)
            .MaximumLength(50)
            .WithMessage("Operator ID cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.OperatorId));

        // Business rule validations
        RuleFor(x => x.ScheduledEndTime)
            .Must((command, endTime) => (endTime - command.ScheduledStartTime).TotalHours <= 24)
            .WithMessage("Work order duration cannot exceed 24 hours")
            .When(x => x.ScheduledStartTime != default && x.ScheduledEndTime != default);
    }

    /// <summary>
    /// Validates that DateTime is valid and not default
    /// </summary>
    /// <param name="dateTime">DateTime to validate</param>
    /// <returns>True if valid</returns>
    private static bool BeValidDateTime(DateTime dateTime)
    {
        return dateTime != default && dateTime > DateTime.MinValue && dateTime < DateTime.MaxValue;
    }
}
