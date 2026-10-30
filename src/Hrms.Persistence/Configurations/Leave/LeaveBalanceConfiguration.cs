using Hrms.Leave.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Leave;

public class LeaveBalanceConfiguration : IEntityTypeConfiguration<LeaveBalance>
{
    public void Configure(EntityTypeBuilder<LeaveBalance> builder)
    {
        builder.ToTable("LeaveBalances", t =>
        {
            t.HasCheckConstraint("CK_LeaveBalances_Values", "[Entitled] >= 0 AND [Used] >= 0 AND [Used] <= [Entitled]");
        });

        builder.HasKey(lb => lb.LeaveBalanceId)
            .HasName("PK_LeaveBalances");

        builder.Property(lb => lb.LeaveBalanceId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(lb => lb.EmployeeId)
            .IsRequired();

        builder.Property(lb => lb.LeaveTypeId)
            .IsRequired();

        builder.Property(lb => lb.Year)
            .HasColumnName("Year")
            .IsRequired();

        builder.Property(lb => lb.Entitled)
            .HasColumnType("decimal(4,1)")
            .IsRequired();

        builder.Property(lb => lb.Used)
            .HasColumnType("decimal(4,1)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(lb => lb.Remaining)
            .HasColumnType("decimal(4,1)")
            .HasComputedColumnSql("[Entitled] - [Used]", stored: true);

        builder.Property(lb => lb.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(lb => lb.UpdatedByUserId);

        builder.Property(lb => lb.RowVersion)
            .IsRowVersion();

        builder.HasIndex(lb => new { lb.EmployeeId, lb.LeaveTypeId, lb.Year })
            .IsUnique()
            .HasDatabaseName("UQ_LeaveBalances_Emp_Type_Year");

        builder.HasIndex(lb => lb.LeaveTypeId)
            .HasDatabaseName("IX_LeaveBalances_LeaveType");

        builder.HasOne(lb => lb.LeaveType)
            .WithMany()
            .HasForeignKey(lb => lb.LeaveTypeId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_LeaveBalances_LeaveTypes");
    }
}
