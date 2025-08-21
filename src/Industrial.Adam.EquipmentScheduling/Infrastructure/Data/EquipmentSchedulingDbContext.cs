using System.Reflection;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Data;

/// <summary>
/// Entity Framework DbContext for Equipment Scheduling
/// </summary>
public sealed class EquipmentSchedulingDbContext : DbContext
{
    private readonly ILogger<EquipmentSchedulingDbContext> _logger;

    public EquipmentSchedulingDbContext(
        DbContextOptions<EquipmentSchedulingDbContext> options,
        ILogger<EquipmentSchedulingDbContext> logger) : base(options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the Resources DbSet
    /// </summary>
    public DbSet<Resource> Resources { get; set; } = null!;

    /// <summary>
    /// Gets or sets the OperatingPatterns DbSet
    /// </summary>
    public DbSet<OperatingPattern> OperatingPatterns { get; set; } = null!;

    /// <summary>
    /// Gets or sets the PatternAssignments DbSet
    /// </summary>
    public DbSet<PatternAssignment> PatternAssignments { get; set; } = null!;

    /// <summary>
    /// Gets or sets the EquipmentSchedules DbSet
    /// </summary>
    public DbSet<EquipmentSchedule> EquipmentSchedules { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure schema name
        modelBuilder.HasDefaultSchema("equipment_scheduling");

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            _logger.LogWarning("DbContext is not configured with connection string");
        }

        // Enable sensitive data logging in development
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Saves changes and publishes domain events
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected rows</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = new List<IDomainEvent>();

        // Collect domain events from aggregate roots
        foreach (var entry in ChangeTracker.Entries<IAggregateRoot>())
        {
            if (entry.Entity.DomainEvents.Any())
            {
                domainEvents.AddRange(entry.Entity.DomainEvents);
                entry.Entity.ClearDomainEvents();
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // TODO: Publish domain events using a domain event dispatcher
        // This would typically be handled by a separate service
        foreach (var domainEvent in domainEvents)
        {
            _logger.LogDebug("Domain event {EventType} occurred for entity {EntityId}",
                domainEvent.GetType().Name, domainEvent.Id);
        }

        return result;
    }
}
