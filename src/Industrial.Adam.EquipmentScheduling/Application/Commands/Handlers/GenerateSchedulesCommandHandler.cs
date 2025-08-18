using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Commands.Handlers;

/// <summary>
/// Handler for schedule generation commands
/// </summary>
public sealed class GenerateSchedulesCommandHandler :
    IRequestHandler<GenerateSchedulesCommand, IEnumerable<EquipmentScheduleDto>>,
    IRequestHandler<RegenerateSchedulesCommand, IEnumerable<EquipmentScheduleDto>>,
    IRequestHandler<CreateExceptionScheduleCommand, EquipmentScheduleDto>,
    IRequestHandler<UpdateEquipmentScheduleCommand, EquipmentScheduleDto>,
    IRequestHandler<CancelEquipmentScheduleCommand, Unit>,
    IRequestHandler<CompleteEquipmentScheduleCommand, Unit>
{
    private readonly IEquipmentScheduleRepository _scheduleRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IPatternAssignmentRepository _assignmentRepository;
    private readonly ISchedulingService _schedulingService;
    private readonly ILogger<GenerateSchedulesCommandHandler> _logger;

    public GenerateSchedulesCommandHandler(
        IEquipmentScheduleRepository scheduleRepository,
        IResourceRepository resourceRepository,
        IPatternAssignmentRepository assignmentRepository,
        ISchedulingService schedulingService,
        ILogger<GenerateSchedulesCommandHandler> logger)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _resourceRepository = resourceRepository ?? throw new ArgumentNullException(nameof(resourceRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _schedulingService = schedulingService ?? throw new ArgumentNullException(nameof(schedulingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<EquipmentScheduleDto>> Handle(GenerateSchedulesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            request.ResourceId, request.StartDate, request.EndDate);

        // Validate resource exists
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
        {
            throw new ArgumentException($"Resource with ID {request.ResourceId} not found", nameof(request.ResourceId));
        }

        if (!resource.RequiresScheduling)
        {
            throw new InvalidOperationException($"Resource {resource.Name} does not require scheduling");
        }

        // Use the domain service to generate schedules
        var schedules = await _schedulingService.GenerateSchedulesAsync(
            request.ResourceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Save the generated schedules
        await _scheduleRepository.AddRangeAsync(schedules, cancellationToken);

        _logger.LogInformation("Generated {Count} schedules for resource {ResourceId}",
            schedules.Count(), request.ResourceId);

        // Convert to DTOs
        return schedules.Select(s => new EquipmentScheduleDto
        {
            Id = s.Id,
            ResourceId = s.ResourceId,
            ScheduleDate = s.ScheduleDate,
            ShiftCode = s.ShiftCode,
            PlannedStartTime = s.PlannedStartTime,
            PlannedEndTime = s.PlannedEndTime,
            PlannedHours = s.PlannedHours,
            ScheduleStatus = s.Status,
            PatternId = s.PatternId,
            IsException = s.IsException,
            GeneratedAt = s.GeneratedAt
        });
    }

    public async Task<IEnumerable<EquipmentScheduleDto>> Handle(RegenerateSchedulesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Regenerating schedules for resource {ResourceId} from {StartDate} to {EndDate}",
            request.ResourceId, request.StartDate, request.EndDate);

        // Delete existing schedules in the date range
        await _scheduleRepository.DeleteByResourceAndDateRangeAsync(
            request.ResourceId,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        // Generate new schedules
        var generateCommand = new GenerateSchedulesCommand(request.ResourceId, request.StartDate, request.EndDate);
        return await Handle(generateCommand, cancellationToken);
    }

    public async Task<EquipmentScheduleDto> Handle(CreateExceptionScheduleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating exception schedule for resource {ResourceId} on {Date}",
            request.ResourceId, request.Date);

        // Validate resource exists
        var resource = await _resourceRepository.GetByIdAsync(request.ResourceId, cancellationToken);
        if (resource == null)
        {
            throw new ArgumentException($"Resource with ID {request.ResourceId} not found", nameof(request.ResourceId));
        }

        // Check for conflicts
        var conflicts = await _scheduleRepository.GetConflictingSchedulesAsync(
            request.ResourceId,
            request.Date,
            request.StartTime,
            request.EndTime,
            null,
            cancellationToken);

        if (conflicts.Any())
        {
            throw new InvalidOperationException("Exception schedule conflicts with existing schedules");
        }

        // Calculate planned hours
        var plannedHours = (decimal)(request.EndTime - request.StartTime).TotalHours;

        // Create exception schedule
        var schedule = new EquipmentSchedule(
            request.ResourceId,
            request.Date,
            plannedHours,
            null, // No pattern for exceptions
            null, // No shift code for exceptions
            request.StartTime,
            request.EndTime,
            true); // Is exception

        schedule.MarkAsException(request.Reason);

        await _scheduleRepository.AddAsync(schedule, cancellationToken);

        _logger.LogInformation("Created exception schedule {ScheduleId} for resource {ResourceId}",
            schedule.Id, request.ResourceId);

        return new EquipmentScheduleDto
        {
            Id = schedule.Id,
            ResourceId = schedule.ResourceId,
            ScheduleDate = schedule.ScheduleDate,
            ShiftCode = schedule.ShiftCode,
            PlannedStartTime = schedule.PlannedStartTime,
            PlannedEndTime = schedule.PlannedEndTime,
            PlannedHours = schedule.PlannedHours,
            ScheduleStatus = schedule.Status,
            PatternId = schedule.PatternId,
            IsException = schedule.IsException,
            GeneratedAt = schedule.GeneratedAt
        };
    }

    public async Task<EquipmentScheduleDto> Handle(UpdateEquipmentScheduleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating equipment schedule {ScheduleId}", request.Id);

        var schedule = await _scheduleRepository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule == null)
        {
            throw new ArgumentException($"Equipment schedule with ID {request.Id} not found", nameof(request.Id));
        }

        // Update the schedule
        schedule.UpdateSchedule(
            request.PlannedHours,
            request.PlannedStartTime,
            request.PlannedEndTime,
            request.ShiftCode,
            request.Notes);

        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);

        _logger.LogInformation("Updated equipment schedule {ScheduleId}", request.Id);

        return new EquipmentScheduleDto
        {
            Id = schedule.Id,
            ResourceId = schedule.ResourceId,
            ScheduleDate = schedule.ScheduleDate,
            ShiftCode = schedule.ShiftCode,
            PlannedStartTime = schedule.PlannedStartTime,
            PlannedEndTime = schedule.PlannedEndTime,
            PlannedHours = schedule.PlannedHours,
            ScheduleStatus = schedule.Status,
            PatternId = schedule.PatternId,
            IsException = schedule.IsException,
            GeneratedAt = schedule.GeneratedAt
        };
    }

    public async Task<Unit> Handle(CancelEquipmentScheduleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling equipment schedule {ScheduleId}", request.ScheduleId);

        var schedule = await _scheduleRepository.GetByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule == null)
        {
            throw new ArgumentException($"Equipment schedule with ID {request.ScheduleId} not found", nameof(request.ScheduleId));
        }

        schedule.Cancel(request.Reason);
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);

        _logger.LogInformation("Cancelled equipment schedule {ScheduleId}", request.ScheduleId);

        return Unit.Value;
    }

    public async Task<Unit> Handle(CompleteEquipmentScheduleCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing equipment schedule {ScheduleId}", request.ScheduleId);

        var schedule = await _scheduleRepository.GetByIdAsync(request.ScheduleId, cancellationToken);
        if (schedule == null)
        {
            throw new ArgumentException($"Equipment schedule with ID {request.ScheduleId} not found", nameof(request.ScheduleId));
        }

        schedule.Complete();
        await _scheduleRepository.UpdateAsync(schedule, cancellationToken);

        _logger.LogInformation("Completed equipment schedule {ScheduleId}", request.ScheduleId);

        return Unit.Value;
    }
}
