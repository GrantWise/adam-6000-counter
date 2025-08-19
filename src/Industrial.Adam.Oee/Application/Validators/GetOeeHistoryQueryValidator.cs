using FluentValidation;
using Industrial.Adam.Oee.Application.Queries;

namespace Industrial.Adam.Oee.Application.Validators;

/// <summary>
/// Validator for GetOeeHistoryQuery
/// </summary>
public class GetOeeHistoryQueryValidator : AbstractValidator<GetOeeHistoryQuery>
{
    /// <summary>
    /// Initializes a new instance of the GetOeeHistoryQueryValidator class
    /// </summary>
    public GetOeeHistoryQueryValidator()
    {
        RuleFor(x => x.DeviceId)
            .NotEmpty()
            .WithMessage("Device ID is required")
            .MaximumLength(20)
            .WithMessage("Device ID cannot exceed 20 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Device ID can only contain alphanumeric characters, underscores, and hyphens");

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .WithMessage("Start time is required")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("Start time cannot be in the future");

        RuleFor(x => x.EndTime)
            .NotEmpty()
            .WithMessage("End time is required")
            .GreaterThan(x => x.StartTime)
            .WithMessage("End time must be after start time")
            .LessThanOrEqualTo(DateTime.UtcNow)
            .WithMessage("End time cannot be in the future");

        RuleFor(x => x.MaxRecords)
            .GreaterThan(0)
            .WithMessage("Max records must be greater than zero")
            .LessThanOrEqualTo(1000)
            .WithMessage("Max records cannot exceed 1,000")
            .When(x => x.MaxRecords.HasValue);

        // Business rule: history period should not exceed 90 days
        RuleFor(x => x.EndTime)
            .Must((query, endTime) => (endTime - query.StartTime).TotalDays <= 90)
            .WithMessage("History period cannot exceed 90 days")
            .When(x => x.StartTime != default);
    }
}
