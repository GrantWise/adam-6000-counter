using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Industrial.Adam.EquipmentScheduling.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation of IOperatingPatternRepository
/// </summary>
public sealed class OperatingPatternRepository : IOperatingPatternRepository
{
    private readonly EquipmentSchedulingDbContext _context;
    private readonly ILogger<OperatingPatternRepository> _logger;

    public OperatingPatternRepository(
        EquipmentSchedulingDbContext context,
        ILogger<OperatingPatternRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<OperatingPattern?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting operating pattern by ID {PatternId}", id);

        return await _context.OperatingPatterns
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<OperatingPattern?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        _logger.LogDebug("Getting operating pattern by name {Name}", name);

        return await _context.OperatingPatterns
            .FirstOrDefaultAsync(p => p.Name == name.Trim(), cancellationToken);
    }

    public async Task<IEnumerable<OperatingPattern>> GetByTypeAsync(PatternType type, bool visibleOnly = true, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting operating patterns by type {Type}, visibleOnly: {VisibleOnly}", type, visibleOnly);

        var query = _context.OperatingPatterns
            .Where(p => p.Type == type);

        if (visibleOnly)
        {
            query = query.Where(p => p.IsVisible);
        }

        return await query
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OperatingPattern>> GetVisiblePatternsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all visible operating patterns");

        return await _context.OperatingPatterns
            .Where(p => p.IsVisible)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OperatingPattern>> GetByWeeklyHoursRangeAsync(decimal minHours, decimal maxHours, bool visibleOnly = true, CancellationToken cancellationToken = default)
    {
        if (minHours < 0)
            throw new ArgumentException("Minimum hours cannot be negative", nameof(minHours));

        if (maxHours < minHours)
            throw new ArgumentException("Maximum hours cannot be less than minimum hours", nameof(maxHours));

        _logger.LogDebug("Getting operating patterns by weekly hours range {MinHours}-{MaxHours}, visibleOnly: {VisibleOnly}",
            minHours, maxHours, visibleOnly);

        var query = _context.OperatingPatterns
            .Where(p => p.WeeklyHours >= minHours && p.WeeklyHours <= maxHours);

        if (visibleOnly)
        {
            query = query.Where(p => p.IsVisible);
        }

        return await query
            .OrderBy(p => p.WeeklyHours)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OperatingPattern pattern, CancellationToken cancellationToken = default)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        _logger.LogDebug("Adding operating pattern {Name}", pattern.Name);

        await _context.OperatingPatterns.AddAsync(pattern, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added operating pattern {PatternId} with name {Name}", pattern.Id, pattern.Name);
    }

    public async Task UpdateAsync(OperatingPattern pattern, CancellationToken cancellationToken = default)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        _logger.LogDebug("Updating operating pattern {PatternId}", pattern.Id);

        _context.OperatingPatterns.Update(pattern);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated operating pattern {PatternId}", pattern.Id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        _logger.LogDebug("Checking if operating pattern name {Name} exists, excludeId: {ExcludeId}", name, excludeId);

        var query = _context.OperatingPatterns
            .Where(p => p.Name == name.Trim());

        if (excludeId.HasValue)
        {
            query = query.Where(p => p.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
