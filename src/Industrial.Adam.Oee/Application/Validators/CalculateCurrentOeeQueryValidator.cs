using FluentValidation;
using Industrial.Adam.Oee.Application.Queries;

namespace Industrial.Adam.Oee.Application.Validators;

/// <summary>
/// Validator for CalculateCurrentOeeQuery
/// </summary>
public class CalculateCurrentOeeQueryValidator : AbstractValidator<CalculateCurrentOeeQuery>
{
    /// <summary>
    /// Initializes a new instance of the CalculateCurrentOeeQueryValidator class
    /// </summary>
    public CalculateCurrentOeeQueryValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required")
            .MaximumLength(20)
            .WithMessage("Device ID cannot exceed 20 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Device ID can only contain alphanumeric characters, underscores, and hyphens");

        RuleFor(x => x.StartTime)
            .Must(BeValidDateTime)
            .WithMessage("Start time must be a valid date and time")
            .When(x => x.StartTime.HasValue);

        RuleFor(x => x.EndTime)
            .Must(BeValidDateTime)
            .WithMessage("End time must be a valid date and time")
            .When(x => x.EndTime.HasValue)
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);

        // Business rule: calculation period should not exceed 30 days
        RuleFor(x => x.EndTime)
            .Must((query, endTime) => !endTime.HasValue || !query.StartTime.HasValue ||
                                     (endTime.Value - query.StartTime.Value).TotalDays <= 30)
            .WithMessage("Calculation period cannot exceed 30 days")
            .When(x => x.StartTime.HasValue && x.EndTime.HasValue);

        // Business rule: times should not be in the future
        RuleFor(x => x.StartTime)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Start time cannot be in the future")
            .When(x => x.StartTime.HasValue);

        RuleFor(x => x.EndTime)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("End time cannot be in the future")
            .When(x => x.EndTime.HasValue);
    }

    /// <summary>
    /// Validates that DateTime is valid
    /// </summary>
    /// <param name="dateTime">DateTime to validate</param>
    /// <returns>True if valid</returns>
    private static bool BeValidDateTime(DateTime? dateTime)
    {
        return !dateTime.HasValue ||
               (dateTime.Value > DateTime.MinValue && dateTime.Value < DateTime.MaxValue);
    }
}
