using Industrial.Adam.EquipmentScheduling.Domain.Entities;
using Industrial.Adam.EquipmentScheduling.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Industrial.Adam.EquipmentScheduling.Infrastructure.Data.Configurations;

/// <summary>
/// Entity Framework configuration for Resource entity
/// </summary>
public sealed class ResourceConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        // Table mapping
        builder.ToTable("sched_resources");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Properties
        builder.Property(r => r.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(r => r.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.Type)
            .HasColumnName("resource_type")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.ParentId)
            .HasColumnName("parent_id");

        builder.Property(r => r.HierarchyPath)
            .HasColumnName("hierarchy_path")
            .HasMaxLength(500);

        builder.Property(r => r.RequiresScheduling)
            .HasColumnName("requires_scheduling")
            .HasDefaultValue(false);

        builder.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("NOW()");

        // Indexes
        builder.HasIndex(r => r.Code)
            .HasDatabaseName("idx_sched_resources_code")
            .IsUnique();

        builder.HasIndex(r => r.ParentId)
            .HasDatabaseName("idx_sched_resources_parent");

        builder.HasIndex(r => r.HierarchyPath)
            .HasDatabaseName("idx_sched_resources_hierarchy");

        builder.HasIndex(r => r.Type)
            .HasDatabaseName("idx_sched_resources_type");

        builder.HasIndex(r => r.RequiresScheduling)
            .HasDatabaseName("idx_sched_resources_scheduling")
            .HasFilter("requires_scheduling = true");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("idx_sched_resources_active")
            .HasFilter("is_active = true");

        // Relationships
        builder.HasMany(r => r.Children)
            .WithOne()
            .HasForeignKey(r => r.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(r => r.PatternAssignments)
            .WithOne(pa => pa.Resource)
            .HasForeignKey(pa => pa.ResourceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Self-referencing foreign key constraint
        builder.HasOne<Resource>()
            .WithMany()
            .HasForeignKey(r => r.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore domain events collection (not persisted)
        builder.Ignore(r => r.DomainEvents);
    }
}
