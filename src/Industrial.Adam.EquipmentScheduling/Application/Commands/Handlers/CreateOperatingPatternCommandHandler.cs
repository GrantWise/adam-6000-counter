using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands.Handlers;

/// <summary>
/// Handler for operating pattern commands
/// </summary>
public sealed class CreateOperatingPatternCommandHandler :
    IRequestHandler<CreateOperatingPatternCommand, OperatingPatternDto>,
    IRequestHandler<UpdateOperatingPatternCommand, OperatingPatternDto>,
    IRequestHandler<HideOperatingPatternCommand, Unit>,
    IRequestHandler<ShowOperatingPatternCommand, Unit>
{
    private readonly IOperatingPatternRepository _patternRepository;
    private readonly ILogger<CreateOperatingPatternCommandHandler> _logger;

    public CreateOperatingPatternCommandHandler(
        IOperatingPatternRepository patternRepository,
        ILogger<CreateOperatingPatternCommandHandler> logger)
    {
        _patternRepository = patternRepository ?? throw new ArgumentNullException(nameof(patternRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperatingPatternDto> Handle(CreateOperatingPatternCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating operating pattern '{Name}' of type {Type}", request.Name, request.Type);

        // Check if name already exists
        if (await _patternRepository.ExistsByNameAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException($"Operating pattern with name '{request.Name}' already exists");
        }

        // Create the pattern
        var pattern = new OperatingPattern(
            request.Name,
            request.Type,
            request.CycleDays,
            request.WeeklyHours,
            request.Configuration,
            request.Description);

        await _patternRepository.AddAsync(pattern, cancellationToken);

        _logger.LogInformation("Created operating pattern {PatternId} with name '{Name}'", pattern.Id, pattern.Name);

        return MapToDto(pattern);
    }

    public async Task<OperatingPatternDto> Handle(UpdateOperatingPatternCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating operating pattern {PatternId}", request.Id);

        var pattern = await _patternRepository.GetByIdAsync(request.Id, cancellationToken);
        if (pattern == null)
        {
            throw new InvalidOperationException($"Operating pattern with ID {request.Id} not found");
        }

        // Check if name conflicts with another pattern
        if (await _patternRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken))
        {
            throw new InvalidOperationException($"Operating pattern with name '{request.Name}' already exists");
        }

        pattern.UpdatePattern(
            request.Name,
            request.CycleDays,
            request.WeeklyHours,
            request.Configuration,
            request.Description);

        await _patternRepository.UpdateAsync(pattern, cancellationToken);

        _logger.LogInformation("Updated operating pattern {PatternId}", pattern.Id);

        return MapToDto(pattern);
    }

    public async Task<Unit> Handle(HideOperatingPatternCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Hiding operating pattern {PatternId}", request.PatternId);

        var pattern = await _patternRepository.GetByIdAsync(request.PatternId, cancellationToken);
        if (pattern == null)
        {
            throw new InvalidOperationException($"Operating pattern with ID {request.PatternId} not found");
        }

        pattern.Hide();
        await _patternRepository.UpdateAsync(pattern, cancellationToken);

        _logger.LogInformation("Hidden operating pattern {PatternId}", pattern.Id);

        return Unit.Value;
    }

    public async Task<Unit> Handle(ShowOperatingPatternCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Showing operating pattern {PatternId}", request.PatternId);

        var pattern = await _patternRepository.GetByIdAsync(request.PatternId, cancellationToken);
        if (pattern == null)
        {
            throw new InvalidOperationException($"Operating pattern with ID {request.PatternId} not found");
        }

        pattern.Show();
        await _patternRepository.UpdateAsync(pattern, cancellationToken);

        _logger.LogInformation("Shown operating pattern {PatternId}", pattern.Id);

        return Unit.Value;
    }

    private static OperatingPatternDto MapToDto(OperatingPattern pattern)
    {
        return new OperatingPatternDto
        {
            Id = pattern.Id,
            Name = pattern.Name,
            Type = pattern.Type,
            CycleDays = pattern.CycleDays,
            WeeklyHours = pattern.WeeklyHours,
            Configuration = pattern.Configuration,
            IsVisible = pattern.IsVisible,
            Description = pattern.Description,
            CreatedAt = pattern.CreatedAt,
            UpdatedAt = pattern.UpdatedAt
        };
    }
}
