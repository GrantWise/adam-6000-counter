using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PatternAssignment entity
/// </summary>
public sealed class PatternAssignmentConfiguration : IEntityTypeConfiguration<PatternAssignment>
{
    public void Configure(EntityTypeBuilder<PatternAssignment> builder)
    {
        // Table mapping
        builder.ToTable("sched_pattern_assignments");

        // Primary key
        builder.HasKey(pa => pa.Id);
        builder.Property(pa => pa.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(pa => pa.ResourceId)
            .HasColumnName("resource_id")
            .IsRequired();

        builder.Property(pa => pa.PatternId)
            .HasColumnName("pattern_id")
            .IsRequired();

        builder.Property(pa => pa.EffectiveDate)
            .HasColumnName("effective_date")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(pa => pa.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(pa => pa.IsOverride)
            .HasColumnName("is_override")
            .HasDefaultValue(false);

        builder.Property(pa => pa.AssignedBy)
            .HasColumnName("assigned_by")
            .HasMaxLength(100);

        builder.Property(pa => pa.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(pa => pa.Notes)
            .HasColumnName("notes")
            .HasMaxLength(500);

        builder.Property(pa => pa.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(pa => pa.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(pa => pa.ResourceId)
            .HasDatabaseName("idx_sched_assignments_resource");

        builder.HasIndex(pa => pa.PatternId)
            .HasDatabaseName("idx_sched_assignments_pattern");

        builder.HasIndex(pa => new { pa.ResourceId, pa.EffectiveDate })
            .HasDatabaseName("idx_sched_assignments_resource_effective");

        builder.HasIndex(pa => new { pa.ResourceId, pa.EndDate })
            .HasDatabaseName("idx_sched_assignments_resource_end");

        builder.HasIndex(pa => pa.IsOverride)
            .HasDatabaseName("idx_sched_assignments_override")
            .HasFilter("is_override = true");

        // Relationships
        builder.HasOne(pa => pa.Resource)
            .WithMany(r => r.PatternAssignments)
            .HasForeignKey(pa => pa.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pa => pa.OperatingPattern)
            .WithMany(p => p.Assignments)
            .HasForeignKey(pa => pa.PatternId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore domain events collection (not persisted)
        builder.Ignore(pa => pa.DomainEvents);
    }
}
