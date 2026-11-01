using Hrms.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Attendance;

public class AttendanceRequestConfiguration : IEntityTypeConfiguration<AttendanceRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceRequest> builder)
    {
        builder.ToTable("AttendanceRequests");

        builder.HasKey(r => r.AttendanceRequestId)
            .HasName("PK_AttendanceRequests");

        builder.Property(r => r.AttendanceRequestId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(r => r.EmployeeId)
            .IsRequired();

        builder.Property(r => r.WorkDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(r => r.RequestedCheckInUtc)
            .HasColumnType("datetime2(3)")
            .IsRequired();

        builder.Property(r => r.RequestedCheckOutUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.ReviewRemarks)
            .HasMaxLength(500);

        builder.Property(r => r.ReviewedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.HasIndex(r => new { r.EmployeeId, r.WorkDate })
            .HasDatabaseName("IX_AttendanceRequests_Employee_Date");

        builder.HasIndex(r => r.Status)
            .HasDatabaseName("IX_AttendanceRequests_Status");
    }
}
