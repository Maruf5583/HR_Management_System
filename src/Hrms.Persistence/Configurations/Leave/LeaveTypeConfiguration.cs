using Hrms.Leave.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Leave;

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> builder)
    {
        builder.ToTable("LeaveTypes", t =>
        {
            t.HasCheckConstraint("CK_LeaveTypes_Days", "[DaysPerYear] >= 0");
            t.HasCheckConstraint("CK_LeaveTypes_Status", "[Status] IN ('Active','Inactive')");
        });

        builder.HasKey(lt => lt.LeaveTypeId)
            .HasName("PK_LeaveTypes");

        builder.Property(lt => lt.LeaveTypeId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(lt => lt.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(lt => lt.Name)
            .IsUnique()
            .HasDatabaseName("UQ_LeaveTypes_Name");

        builder.Property(lt => lt.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(lt => lt.Code)
            .IsUnique()
            .HasDatabaseName("UQ_LeaveTypes_Code");

        builder.Property(lt => lt.IsPaid)
            .IsRequired();

        builder.Property(lt => lt.DaysPerYear)
            .HasColumnType("decimal(4,1)")
            .IsRequired();

        builder.Property(lt => lt.RequiresDocument)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(lt => lt.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Active'")
            .IsRequired();

        builder.Property(lt => lt.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(lt => lt.CreatedByUserId);

        builder.Property(lt => lt.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(lt => lt.UpdatedByUserId);
    }
}
