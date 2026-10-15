using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Employees;

public class DesignationConfiguration : IEntityTypeConfiguration<Designation>
{
    public void Configure(EntityTypeBuilder<Designation> builder)
    {
        builder.ToTable("Designations", t =>
        {
            t.HasCheckConstraint("CK_Designations_Status", "[Status] IN ('Active','Inactive')");
        });

        builder.HasKey(d => d.DesignationId);

        builder.Property(d => d.DesignationId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(d => d.Title)
            .HasMaxLength(120)
            .IsRequired();

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

        // Unique constraints
        builder.HasIndex(d => new { d.DepartmentId, d.Title })
            .IsUnique()
            .HasDatabaseName("UQ_Designations_Dept_Title");

        // Target of composite foreign key from Employees
        builder.HasAlternateKey(d => new { d.DesignationId, d.DepartmentId })
            .HasName("UQ_Designations_Id_Dept");

        // Foreign key to Department
        builder.HasOne(d => d.Department)
            .WithMany(dept => dept.Designations)
            .HasForeignKey(d => d.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Designations_Departments");
    }
}
