using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Employees;

public class EmployeeBankDetailConfiguration : IEntityTypeConfiguration<EmployeeBankDetail>
{
    public void Configure(EntityTypeBuilder<EmployeeBankDetail> builder)
    {
        builder.ToTable("EmployeeBankDetails");

        builder.HasKey(b => b.EmployeeBankDetailId)
            .HasName("PK_EmployeeBankDetails");

        builder.Property(b => b.EmployeeBankDetailId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(b => b.EmployeeId)
            .IsRequired();

        builder.Property(b => b.BankName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.AccountHolderName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.AccountNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(b => b.BranchName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.RoutingNumber)
            .HasMaxLength(50);

        builder.Property(b => b.SwiftCode)
            .HasMaxLength(50);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(b => b.IsActive)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(b => b.ReviewRemarks)
            .HasMaxLength(500);

        builder.Property(b => b.ReviewedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(b => b.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(b => b.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.HasIndex(b => b.EmployeeId)
            .HasDatabaseName("IX_EmployeeBankDetails_EmployeeId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_EmployeeBankDetails_Status");

        builder.HasOne(b => b.Employee)
            .WithMany(e => e.BankDetails)
            .HasForeignKey(b => b.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_EmployeeBankDetails_Employees");
    }
}
