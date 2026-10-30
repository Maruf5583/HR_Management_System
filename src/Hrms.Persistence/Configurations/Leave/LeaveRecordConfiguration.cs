using Hrms.Leave.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Leave;

public class LeaveRecordConfiguration : IEntityTypeConfiguration<LeaveRecord>
{
    public void Configure(EntityTypeBuilder<LeaveRecord> builder)
    {
        builder.ToTable("LeaveRecords", t =>
        {
            t.HasCheckConstraint("CK_LeaveRecords_Range", "[EndDate] >= [StartDate]");
            t.HasCheckConstraint("CK_LeaveRecords_Days", "[TotalDays] > 0");
            t.HasCheckConstraint("CK_LeaveRecords_Half", "[IsHalfDay] = 0 OR ([StartDate] = [EndDate] AND [TotalDays] = 0.5)");
            t.HasCheckConstraint("CK_LeaveRecords_Status", "[Status] IN ('Pending','Approved','Rejected','Cancelled')");
            t.HasCheckConstraint("CK_LeaveRecords_Reviewer", "([Status] NOT IN ('Approved','Rejected')) OR ([ReviewedByUserId] IS NOT NULL AND [ReviewedAtUtc] IS NOT NULL)");
        });

        builder.HasKey(lr => lr.LeaveRecordId)
            .HasName("PK_LeaveRecords");

        builder.Property(lr => lr.LeaveRecordId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(lr => lr.EmployeeId)
            .IsRequired();

        builder.Property(lr => lr.LeaveTypeId)
            .IsRequired();

        builder.Property(lr => lr.StartDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(lr => lr.EndDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(lr => lr.IsHalfDay)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(lr => lr.TotalDays)
            .HasColumnType("decimal(4,1)")
            .IsRequired();

        builder.Property(lr => lr.Reason)
            .HasMaxLength(1000);

        builder.Property(lr => lr.AttachmentKey)
            .HasMaxLength(500);

        builder.Property(lr => lr.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Approved'")
            .IsRequired();

        builder.Property(lr => lr.ReviewedByUserId);

        builder.Property(lr => lr.ReviewedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(lr => lr.ReviewNote)
            .HasMaxLength(1000);

        builder.Property(lr => lr.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(lr => lr.CreatedByUserId);

        builder.Property(lr => lr.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(lr => lr.UpdatedByUserId);

        builder.HasIndex(lr => new { lr.EmployeeId, lr.StartDate })
            .IncludeProperties(lr => new { lr.EndDate, lr.Status })
            .HasDatabaseName("IX_LeaveRecords_Emp_Start");

        builder.HasIndex(lr => lr.LeaveTypeId)
            .HasDatabaseName("IX_LeaveRecords_LeaveType");

        builder.HasOne(lr => lr.LeaveType)
            .WithMany()
            .HasForeignKey(lr => lr.LeaveTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_LeaveRecords_LeaveTypes");
    }
}
