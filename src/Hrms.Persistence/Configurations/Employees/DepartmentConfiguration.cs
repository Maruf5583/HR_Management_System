using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Employees;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments", t =>
        {
            t.HasCheckConstraint("CK_Departments_Status", "[Status] IN ('Active','Inactive')");
        });

        builder.HasKey(d => d.DepartmentId);

        builder.Property(d => d.DepartmentId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(d => d.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("UQ_Departments_Name");

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Active'")
            .IsRequired();

        builder.Property(d => d.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(d => d.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");
    }
}
