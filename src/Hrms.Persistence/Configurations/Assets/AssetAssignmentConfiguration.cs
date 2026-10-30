using Hrms.Assets.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Assets;

public class AssetAssignmentConfiguration : IEntityTypeConfiguration<AssetAssignment>
{
    public void Configure(EntityTypeBuilder<AssetAssignment> builder)
    {
        builder.ToTable("AssetAssignments", t =>
        {
            t.HasCheckConstraint("CK_AssetAssignments_Return", "[ReturnedAtUtc] IS NULL OR [ReturnedAtUtc] >= [AssignedAtUtc]");
        });

        builder.HasKey(a => a.AssetAssignmentId)
            .HasName("PK_AssetAssignments");

        builder.Property(a => a.AssetAssignmentId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.AssignedAtUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(a => a.ReturnedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.Note)
            .HasMaxLength(500);

        builder.HasOne(a => a.Asset)
            .WithMany(asset => asset.Assignments)
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_AssetAssignments_Assets");

        // Unique filtered index: one open assignment per asset
        builder.HasIndex(a => a.AssetId)
            .IsUnique()
            .HasFilter("[ReturnedAtUtc] IS NULL")
            .HasDatabaseName("UX_AssetAssignments_Current");

        // Index on current assignments for employee
        builder.HasIndex(a => a.EmployeeId)
            .HasFilter("[ReturnedAtUtc] IS NULL")
            .HasDatabaseName("IX_AssetAssignments_Employee_Current");
    }
}
