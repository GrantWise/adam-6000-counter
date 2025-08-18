using FluentValidation;
using Industrial.Adam.EquipmentScheduling.Application.Commands;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Application.Validators;

/// <summary>
/// Validator for CreateOperatingPatternCommand
/// </summary>
public sealed class CreateOperatingPatternCommandValidator : AbstractValidator<CreateOperatingPatternCommand>
{
    public CreateOperatingPatternCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pattern name is required")
            .MaximumLength(100)
            .WithMessage("Pattern name cannot exceed 100 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid pattern type");

        RuleFor(x => x.CycleDays)
            .InclusiveBetween(1, 365)
            .WithMessage("Cycle days must be between 1 and 365");

        RuleFor(x => x.WeeklyHours)
            .InclusiveBetween(0, 168)
            .WithMessage("Weekly hours must be between 0 and 168");

        RuleFor(x => x.Configuration)
            .NotNull()
            .WithMessage("Configuration is required");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Custom validation for weekly hours based on pattern type
        RuleFor(x => x)
            .Must(ValidateWeeklyHoursForPatternType)
            .WithMessage("Weekly hours must be appropriate for the pattern type")
            .WithName("WeeklyHours");
    }

    private static bool ValidateWeeklyHoursForPatternType(CreateOperatingPatternCommand command)
    {
        return command.Type switch
        {
            PatternType.Continuous => command.WeeklyHours == 168, // 24 hours * 7 days
            PatternType.TwoShift => command.WeeklyHours <= 80,     // 16 hours * 5 days max
            PatternType.DayOnly => command.WeeklyHours <= 40,      // 8 hours * 5 days max
            PatternType.Extended => command.WeeklyHours <= 60,     // 12 hours * 5 days max
            PatternType.Custom => command.WeeklyHours >= 0,        // Any value allowed for custom
            _ => false
        };
    }
}

/// <summary>
/// Validator for UpdateOperatingPatternCommand
/// </summary>
public sealed class UpdateOperatingPatternCommandValidator : AbstractValidator<UpdateOperatingPatternCommand>
{
    public UpdateOperatingPatternCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Pattern ID must be positive");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Pattern name is required")
            .MaximumLength(100)
            .WithMessage("Pattern name cannot exceed 100 characters");

        RuleFor(x => x.CycleDays)
            .InclusiveBetween(1, 365)
            .WithMessage("Cycle days must be between 1 and 365");

        RuleFor(x => x.WeeklyHours)
            .InclusiveBetween(0, 168)
            .WithMessage("Weekly hours must be between 0 and 168");

        RuleFor(x => x.Configuration)
            .NotNull()
            .WithMessage("Configuration is required");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
