using Hrms.Calendar.Domain.Entities;
using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Calendar;

public class ShiftAssignmentConfiguration : IEntityTypeConfiguration<ShiftAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftAssignment> builder)
    {
        builder.ToTable("ShiftAssignments", t =>
        {
            t.HasCheckConstraint("CK_ShiftAssignments_Range", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });

        builder.HasKey(a => a.ShiftAssignmentId);

        builder.Property(a => a.ShiftAssignmentId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.ShiftId)
            .IsRequired();

        builder.Property(a => a.EffectiveFrom)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.EffectiveTo)
            .HasColumnType("date");

        builder.Property(a => a.AssignedByUserId);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .HasConstraintName("FK_ShiftAssignments_Employees")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EmployeeId, a.EffectiveFrom })
            .IsUnique()
            .HasDatabaseName("UQ_ShiftAssignments_Emp_From");

        builder.HasIndex(a => a.EmployeeId)
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL")
            .HasDatabaseName("UX_ShiftAssignments_Current");

        builder.HasIndex(a => a.ShiftId)
            .HasDatabaseName("IX_ShiftAssignments_Shift");
    }
}
