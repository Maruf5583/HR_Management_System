using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Calendar;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", t =>
        {
            t.HasCheckConstraint("CK_Shifts_Status", "[Status] IN ('Active','Inactive')");
        });

        builder.HasKey(s => s.ShiftId);

        builder.Property(s => s.ShiftId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Shifts_Name");

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Active'")
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(s => s.CreatedByUserId);

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(s => s.UpdatedByUserId);

        builder.Ignore(s => s.CurrentVersion);

        builder.HasMany(s => s.Versions)
            .WithOne(v => v.Shift)
            .HasForeignKey(v => v.ShiftId)
            .HasConstraintName("FK_ShiftVersions_Shifts")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Assignments)
            .WithOne(a => a.Shift)
            .HasForeignKey(a => a.ShiftId)
            .HasConstraintName("FK_ShiftAssignments_Shifts")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
