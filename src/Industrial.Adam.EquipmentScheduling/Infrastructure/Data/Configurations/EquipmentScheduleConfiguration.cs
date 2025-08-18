using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for EquipmentSchedule entity
/// </summary>
public sealed class EquipmentScheduleConfiguration : IEntityTypeConfiguration<EquipmentSchedule>
{
    public void Configure(EntityTypeBuilder<EquipmentSchedule> builder)
    {
        // Table mapping
        builder.ToTable("sched_equipment_schedules");

        // Primary key
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(s => s.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(s => s.ScheduleDate)
            .HasColumnName("schedule_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(s => s.ShiftCode)
            .HasColumnName("shift_code")
            .HasMaxLength(10);

        builder.Property(s => s.PlannedStartTime)
            .HasColumnName("planned_start_time")
            .HasColumnType("timestamptz");

        builder.Property(s => s.PlannedEndTime)
            .HasColumnName("planned_end_time")
            .HasColumnType("timestamptz");

        builder.Property(s => s.PlannedHours)
            .HasColumnName("planned_hours")
            .HasColumnType("decimal(4,2)")
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("schedule_status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(s => s.PatternId)
            .HasColumnName("pattern_id");

        builder.Property(s => s.IsException)
            .HasColumnName("is_exception")
            .HasDefaultValue(false);

        builder.Property(s => s.GeneratedAt)
            .HasColumnName("generated_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(s => s.ResourceId)
            .HasDatabaseName("idx_sched_schedules_resource");

        builder.HasIndex(s => new { s.ResourceId, s.ScheduleDate })
            .HasDatabaseName("idx_sched_schedules_resource_date");

        builder.HasIndex(s => s.ScheduleDate)
            .HasDatabaseName("idx_sched_schedules_date");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("idx_sched_schedules_status");

        builder.HasIndex(s => s.PatternId)
            .HasDatabaseName("idx_sched_schedules_pattern");

        builder.HasIndex(s => s.IsException)
            .HasDatabaseName("idx_sched_schedules_exception")
            .HasFilter("is_exception = true");

        builder.HasIndex(s => new { s.PlannedStartTime, s.PlannedEndTime })
            .HasDatabaseName("idx_sched_schedules_time_range");

        // Relationships
        builder.HasOne(s => s.Resource)
            .WithMany()
            .HasForeignKey(s => s.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.OperatingPattern)
            .WithMany()
            .HasForeignKey(s => s.PatternId)
            .OnDelete(DeleteBehavior.SetNull);

        // Ignore domain events collection (not persisted)
        builder.Ignore(s => s.DomainEvents);
    }
}
