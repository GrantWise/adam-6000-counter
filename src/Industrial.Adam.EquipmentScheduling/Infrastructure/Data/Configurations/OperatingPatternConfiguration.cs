using System.Text.Json;
using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for OperatingPattern entity
/// </summary>
public sealed class OperatingPatternConfiguration : IEntityTypeConfiguration<OperatingPattern>
{
    public void Configure(EntityTypeBuilder<OperatingPattern> builder)
    {
        // Table mapping
        builder.ToTable("sched_operating_patterns");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasColumnName("pattern_type")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.CycleDays)
            .HasColumnName("cycle_days")
            .IsRequired();

        builder.Property(p => p.WeeklyHours)
            .HasColumnName("weekly_hours")
            .HasColumnType("decimal(5,2)");

        builder.Property(p => p.Configuration)
            .HasColumnName("configuration")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v.RootElement.GetRawText(),
                v => JsonDocument.Parse(v, default(JsonDocumentOptions)))
            .IsRequired();

        builder.Property(p => p.IsVisible)
            .HasColumnName("is_visible")
            .HasDefaultValue(true);

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(p => p.Name)
            .HasDatabaseName("idx_sched_patterns_name")
            .IsUnique();

        builder.HasIndex(p => p.Type)
            .HasDatabaseName("idx_sched_patterns_type");

        builder.HasIndex(p => p.WeeklyHours)
            .HasDatabaseName("idx_sched_patterns_weekly_hours");

        builder.HasIndex(p => p.IsVisible)
            .HasDatabaseName("idx_sched_patterns_visible")
            .HasFilter("is_visible = true");

        // Relationships
        builder.HasMany(p => p.Assignments)
            .WithOne(pa => pa.OperatingPattern)
            .HasForeignKey(pa => pa.PatternId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore domain events collection (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}
