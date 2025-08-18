using FluentValidation;
using Industrial.Adam.EquipmentScheduling.Application.Commands;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;

namespace Industrial.Adam.EquipmentScheduling.Application.Validators;

/// <summary>
/// Validator for CreateResourceCommand
/// </summary>
public sealed class CreateResourceCommandValidator : AbstractValidator<CreateResourceCommand>
{
    public CreateResourceCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Resource name is required")
            .MaximumLength(200)
            .WithMessage("Resource name cannot exceed 200 characters");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Resource code is required")
            .MaximumLength(50)
            .WithMessage("Resource code cannot exceed 50 characters")
            .Matches("^[A-Z0-9_-]+$")
            .WithMessage("Resource code can only contain uppercase letters, numbers, hyphens, and underscores");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid resource type");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.ParentId)
            .GreaterThan(0)
            .WithMessage("Parent ID must be positive")
            .When(x => x.ParentId.HasValue);
    }
}

/// <summary>
/// Validator for UpdateResourceCommand
/// </summary>
public sealed class UpdateResourceCommandValidator : AbstractValidator<UpdateResourceCommand>
{
    public UpdateResourceCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Resource ID must be positive");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Resource name is required")
            .MaximumLength(200)
            .WithMessage("Resource name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description cannot exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
