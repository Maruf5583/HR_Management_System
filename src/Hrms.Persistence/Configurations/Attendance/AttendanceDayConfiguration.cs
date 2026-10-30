using Hrms.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Attendance;

public class AttendanceDayConfiguration : IEntityTypeConfiguration<AttendanceDay>
{
    public void Configure(EntityTypeBuilder<AttendanceDay> builder)
    {
        builder.ToTable("AttendanceDays", t =>
        {
            t.HasCheckConstraint("CK_AttendanceDays_Minutes", "[LateMinutes] >= 0 AND [WorkedMinutes] >= 0");
            t.HasCheckConstraint("CK_AttendanceDays_InOut", "[CheckOutUtc] IS NULL OR [CheckInUtc] IS NULL OR [CheckOutUtc] >= [CheckInUtc]");
            t.HasCheckConstraint("CK_AttendanceDays_Status", "[Status] IN ('Present','Late','Absent','HalfDay','OnLeave','Holiday','Weekend')");
        });

        builder.HasKey(a => a.AttendanceDayId)
            .HasName("PK_AttendanceDays");

        builder.Property(a => a.AttendanceDayId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(a => a.EmployeeId)
            .IsRequired();

        builder.Property(a => a.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(a => a.ShiftVersionId);

        builder.Property(a => a.CheckInUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.CheckOutUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.LateMinutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(a => a.WorkedMinutes)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.Note)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(a => a.CreatedByUserId);

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(a => a.UpdatedByUserId);

        builder.Property(a => a.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.HasIndex(a => new { a.EmployeeId, a.WorkDate })
            .IsUnique()
            .HasDatabaseName("UQ_AttendanceDays_Emp_Date");

        builder.HasIndex(a => new { a.WorkDate, a.Status })
            .IncludeProperties(a => a.EmployeeId)
            .HasDatabaseName("IX_AttendanceDays_Date_Status");

        builder.HasIndex(a => a.ShiftVersionId)
            .HasFilter("[ShiftVersionId] IS NOT NULL")
            .HasDatabaseName("IX_AttendanceDays_ShiftVersion");
    }
}
