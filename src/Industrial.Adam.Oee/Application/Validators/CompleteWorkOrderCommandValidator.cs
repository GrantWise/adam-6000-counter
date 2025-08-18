using FluentValidation;
using Industrial.Adam.Oee.Application.Commands;

namespace Industrial.Adam.Oee.Application.Validators;

/// <summary>
/// Validator for CompleteWorkOrderCommand
/// </summary>
public class CompleteWorkOrderCommandValidator : AbstractValidator<CompleteWorkOrderCommand>
{
    public CompleteWorkOrderCommandValidator()
    {
        RuleFor(x => x.WorkOrderId)
            .NotEmpty()
            .WithMessage("Work order ID is required")
            .MaximumLength(50)
            .WithMessage("Work order ID cannot exceed 50 characters");

        RuleFor(x => x.CompletionReason)
            .MaximumLength(500)
            .WithMessage("Completion reason cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.CompletionReason));

        RuleFor(x => x.FinalGoodQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Final good quantity cannot be negative")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Final good quantity cannot exceed 1,000,000")
            .When(x => x.FinalGoodQuantity.HasValue);

        RuleFor(x => x.FinalScrapQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Final scrap quantity cannot be negative")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Final scrap quantity cannot exceed 1,000,000")
            .When(x => x.FinalScrapQuantity.HasValue);

        RuleFor(x => x.CompletedBy)
            .MaximumLength(50)
            .WithMessage("Completed by cannot exceed 50 characters")
            .When(x => !string.IsNullOrEmpty(x.CompletedBy));
    }
}
