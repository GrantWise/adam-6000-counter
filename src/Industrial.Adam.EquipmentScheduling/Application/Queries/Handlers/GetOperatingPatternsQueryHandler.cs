using Industrial.Adam.EquipmentScheduling.Application.DTOs;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Application.Queries.Handlers;

/// <summary>
/// Handler for operating patterns queries
/// </summary>
public sealed class GetOperatingPatternsQueryHandler :
    IRequestHandler<GetOperatingPatternByIdQuery, OperatingPatternDto?>,
    IRequestHandler<GetOperatingPatternByNameQuery, OperatingPatternDto?>,
    IRequestHandler<GetOperatingPatternsByTypeQuery, IEnumerable<OperatingPatternDto>>,
    IRequestHandler<GetVisibleOperatingPatternsQuery, IEnumerable<OperatingPatternDto>>,
    IRequestHandler<GetOperatingPatternsByHoursRangeQuery, IEnumerable<OperatingPatternDto>>,
    IRequestHandler<GetPatternAvailabilityQuery, PatternAvailabilityDto?>,
    IRequestHandler<GetAllPatternAvailabilitiesQuery, IEnumerable<PatternAvailabilityDto>>
{
    private readonly IOperatingPatternRepository _patternRepository;
    private readonly IPatternAssignmentRepository _assignmentRepository;
    private readonly ILogger<GetOperatingPatternsQueryHandler> _logger;

    public GetOperatingPatternsQueryHandler(
        IOperatingPatternRepository patternRepository,
        IPatternAssignmentRepository assignmentRepository,
        ILogger<GetOperatingPatternsQueryHandler> logger)
    {
        _patternRepository = patternRepository ?? throw new ArgumentNullException(nameof(patternRepository));
        _assignmentRepository = assignmentRepository ?? throw new ArgumentNullException(nameof(assignmentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperatingPatternDto?> Handle(GetOperatingPatternByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting operating pattern by ID {PatternId}", request.PatternId);

        var pattern = await _patternRepository.GetByIdAsync(request.PatternId, cancellationToken);
        if (pattern == null)
        {
            return null;
        }

        return new OperatingPatternDto
        {
            Id = pattern.Id,
            Name = pattern.Name,
            Type = pattern.Type,
            CycleDays = pattern.CycleDays,
            WeeklyHours = pattern.WeeklyHours,
            Configuration = pattern.Configuration,
            IsVisible = pattern.IsVisible,
            CreatedAt = pattern.CreatedAt
        };
    }

    public async Task<OperatingPatternDto?> Handle(GetOperatingPatternByNameQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting operating pattern by name {Name}", request.Name);

        var pattern = await _patternRepository.GetByNameAsync(request.Name, cancellationToken);
        if (pattern == null)
        {
            return null;
        }

        return new OperatingPatternDto
        {
            Id = pattern.Id,
            Name = pattern.Name,
            Type = pattern.Type,
            CycleDays = pattern.CycleDays,
            WeeklyHours = pattern.WeeklyHours,
            Configuration = pattern.Configuration,
            IsVisible = pattern.IsVisible,
            CreatedAt = pattern.CreatedAt
        };
    }

    public async Task<IEnumerable<OperatingPatternDto>> Handle(GetOperatingPatternsByTypeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting operating patterns by type {Type}, visibleOnly: {VisibleOnly}",
            request.Type, request.VisibleOnly);

        var patterns = await _patternRepository.GetByTypeAsync(request.Type, request.VisibleOnly, cancellationToken);

        return patterns.Select(p => new OperatingPatternDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            CycleDays = p.CycleDays,
            WeeklyHours = p.WeeklyHours,
            Configuration = p.Configuration,
            IsVisible = p.IsVisible,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<IEnumerable<OperatingPatternDto>> Handle(GetVisibleOperatingPatternsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all visible operating patterns");

        var patterns = await _patternRepository.GetVisiblePatternsAsync(cancellationToken);

        return patterns.Select(p => new OperatingPatternDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            CycleDays = p.CycleDays,
            WeeklyHours = p.WeeklyHours,
            Configuration = p.Configuration,
            IsVisible = p.IsVisible,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<IEnumerable<OperatingPatternDto>> Handle(GetOperatingPatternsByHoursRangeQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting operating patterns by hours range {MinHours}-{MaxHours}, visibleOnly: {VisibleOnly}",
            request.MinHours, request.MaxHours, request.VisibleOnly);

        var patterns = await _patternRepository.GetByWeeklyHoursRangeAsync(
            request.MinHours,
            request.MaxHours,
            request.VisibleOnly,
            cancellationToken);

        return patterns.Select(p => new OperatingPatternDto
        {
            Id = p.Id,
            Name = p.Name,
            Type = p.Type,
            CycleDays = p.CycleDays,
            WeeklyHours = p.WeeklyHours,
            Configuration = p.Configuration,
            IsVisible = p.IsVisible,
            CreatedAt = p.CreatedAt
        });
    }

    public async Task<PatternAvailabilityDto?> Handle(GetPatternAvailabilityQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting pattern availability for pattern {PatternId}", request.PatternId);

        var pattern = await _patternRepository.GetByIdAsync(request.PatternId, cancellationToken);
        if (pattern == null)
        {
            return null;
        }

        // Get active assignments for this pattern
        var activeAssignments = await _assignmentRepository.GetByPatternIdAsync(
            request.PatternId,
            activeOnly: true,
            cancellationToken);

        var dailyAverageHours = pattern.CycleDays > 0
            ? pattern.WeeklyHours * 7m / pattern.CycleDays
            : 0;

        var hoursPerCycle = pattern.WeeklyHours * pattern.CycleDays / 7m;

        return new PatternAvailabilityDto
        {
            PatternId = pattern.Id,
            PatternName = pattern.Name,
            Type = pattern.Type,
            WeeklyHours = pattern.WeeklyHours,
            DailyHours = new Dictionary<DayOfWeek, decimal>(), // Would be populated from pattern configuration
            Shifts = new List<ShiftInfoDto>() // Would be populated from pattern configuration
        };
    }

    public async Task<IEnumerable<PatternAvailabilityDto>> Handle(GetAllPatternAvailabilitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting all pattern availabilities, visibleOnly: {VisibleOnly}", request.VisibleOnly);

        var patterns = request.VisibleOnly
            ? await _patternRepository.GetVisiblePatternsAsync(cancellationToken)
            : await _patternRepository.GetByTypeAsync(Domain.Enums.PatternType.Continuous, false, cancellationToken);

        // For simplicity, get all patterns by getting each type - a proper implementation would have GetAllAsync
        var allPatterns = new List<Domain.Entities.OperatingPattern>();
        foreach (var patternType in Enum.GetValues<Domain.Enums.PatternType>())
        {
            var patternsOfType = await _patternRepository.GetByTypeAsync(patternType, request.VisibleOnly, cancellationToken);
            allPatterns.AddRange(patternsOfType);
        }

        var results = new List<PatternAvailabilityDto>();

        foreach (var pattern in allPatterns.Distinct())
        {
            var activeAssignments = await _assignmentRepository.GetByPatternIdAsync(
                pattern.Id,
                activeOnly: true,
                cancellationToken);

            var dailyAverageHours = pattern.CycleDays > 0
                ? pattern.WeeklyHours * 7m / pattern.CycleDays
                : 0;

            var hoursPerCycle = pattern.WeeklyHours * pattern.CycleDays / 7m;

            results.Add(new PatternAvailabilityDto
            {
                PatternId = pattern.Id,
                PatternName = pattern.Name,
                Type = pattern.Type,
                WeeklyHours = pattern.WeeklyHours,
                DailyHours = new Dictionary<DayOfWeek, decimal>(), // Would be populated from pattern configuration
                Shifts = new List<ShiftInfoDto>() // Would be populated from pattern configuration
            });
        }

        return results.OrderBy(r => r.PatternName);
    }
}
