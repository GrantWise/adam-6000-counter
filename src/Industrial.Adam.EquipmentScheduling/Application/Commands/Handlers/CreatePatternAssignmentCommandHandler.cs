using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands.Handlers;

/// <summary>
/// Handler for pattern assignment commands
/// </summary>
public sealed class CreatePatternAssignmentCommandHandler :
    IRequestHandler<CreatePatternAssignmentCommand, PatternAssignmentDto>,
    IRequestHandler<UpdatePatternAssignmentCommand, PatternAssignmentDto>,
    IRequestHandler<TerminatePatternAssignmentCommand, Unit>
{
    private readonly IPatternAssignmentRepository _assignmentRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IOperatingPatternRepository _patternRepository;
    private readonly ILogger<CreatePatternAssignmentCommandHandler> _logger;

    public CreatePatternAssignmentCommandHandler(
        IPatternAssignmentRepository assignmentRepository,
        IResourceRepository resourceRepository,
        IOperatingPatternRepository patternRepository,
        ILogger<CreatePatternAssignmentCommandHandler> logger)
    {
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _patternRepository = patternRepository ?? throw new ArgumentNullException(nameof(patternRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PatternAssignmentDto> Handle(CreatePatternAssignmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating pattern assignment for resource {ResourceId} with pattern {PatternId}",
            request.ResourceId, request.PatternId);

        // Validate resource exists
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
        {
            throw new ArgumentException($"Resource with ID {request.ResourceId} not found", nameof(request.ResourceId));
        }

        // Validate pattern exists
        var pattern = await _patternRepository.GetByIdAsync(request.PatternId, cancellationToken);
        if (pattern == null)
        {
            throw new ArgumentException($"Operating pattern with ID {request.PatternId} not found", nameof(request.PatternId));
        }

        // Check for conflicting assignments
        var conflictingAssignments = await _assignmentRepository.GetConflictingAssignmentsAsync(
            request.ResourceId,
            request.EffectiveDate,
            request.EndDate,
            null,
            cancellationToken);

        if (conflictingAssignments.Any())
        {
            var conflictDetails = string.Join(", ", conflictingAssignments.Select(ca =>
                $"Assignment {ca.Id} ({ca.EffectiveDate:yyyy-MM-dd} to {ca.EndDate?.ToString("yyyy-MM-dd") ?? "indefinite"})"));

            throw new InvalidOperationException(
                $"Pattern assignment conflicts with existing assignments: {conflictDetails}");
        }

        // Create the assignment
        var assignment = new PatternAssignment(
            request.ResourceId,
            request.PatternId,
            request.EffectiveDate,
            request.EndDate,
            request.IsOverride,
            request.AssignedBy,
            request.Notes);

        await _assignmentRepository.AddAsync(assignment, cancellationToken);

        _logger.LogInformation("Created pattern assignment {AssignmentId} for resource {ResourceId}",
            assignment.Id, request.ResourceId);

        return new PatternAssignmentDto
        {
            Id = assignment.Id,
            ResourceId = assignment.ResourceId,
            PatternId = assignment.PatternId,
            EffectiveDate = assignment.EffectiveDate,
            EndDate = assignment.EndDate,
            IsOverride = assignment.IsOverride,
            AssignedBy = assignment.AssignedBy,
            AssignedAt = assignment.AssignedAt,
            Notes = assignment.Notes
        };
    }

    public async Task<PatternAssignmentDto> Handle(UpdatePatternAssignmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating pattern assignment {AssignmentId}", request.Id);

        var assignment = await _assignmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (assignment == null)
        {
            throw new ArgumentException($"Pattern assignment with ID {request.Id} not found", nameof(request.Id));
        }

        // Update the assignment
        if (request.EndDate.HasValue)
        {
            assignment.UpdateEndDate(request.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Notes))
        {
            assignment.UpdateNotes(request.Notes, request.UpdatedBy);
        }

        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);

        _logger.LogInformation("Updated pattern assignment {AssignmentId}", request.Id);

        return new PatternAssignmentDto
        {
            Id = assignment.Id,
            ResourceId = assignment.ResourceId,
            PatternId = assignment.PatternId,
            EffectiveDate = assignment.EffectiveDate,
            EndDate = assignment.EndDate,
            IsOverride = assignment.IsOverride,
            AssignedBy = assignment.AssignedBy,
            AssignedAt = assignment.AssignedAt,
            Notes = assignment.Notes
        };
    }

    public async Task<Unit> Handle(TerminatePatternAssignmentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Terminating pattern assignment {AssignmentId}", request.AssignmentId);

        var assignment = await _assignmentRepository.GetByIdAsync(request.AssignmentId, cancellationToken);
        if (assignment == null)
        {
            throw new ArgumentException($"Pattern assignment with ID {request.AssignmentId} not found", nameof(request.AssignmentId));
        }

        assignment.Terminate(request.TerminatedBy);
        await _assignmentRepository.UpdateAsync(assignment, cancellationToken);

        _logger.LogInformation("Terminated pattern assignment {AssignmentId}", request.AssignmentId);

        return Unit.Value;
    }
}
