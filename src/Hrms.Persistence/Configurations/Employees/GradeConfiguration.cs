using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Employees;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.ToTable("Grades", t =>
        {
            t.HasCheckConstraint("CK_Grades_Salary", "[Salary] >= 0");
            t.HasCheckConstraint("CK_Grades_Status", "[Status] IN ('Active','Inactive')");
        });

        builder.HasKey(g => g.GradeId);

        builder.Property(g => g.GradeId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(g => g.Level)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(g => g.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(g => g.Salary)
            .HasColumnType("decimal(12,2)")
            .IsRequired();

        builder.Property(g => g.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Active'")
            .IsRequired();

        builder.Property(g => g.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(g => g.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        // Unique constraints
        builder.HasIndex(g => g.Code)
            .IsUnique()
            .HasDatabaseName("UQ_Grades_Code");

        builder.HasIndex(g => new { g.DesignationId, g.Level })
            .IsUnique()
            .HasDatabaseName("UQ_Grades_Designation_Level");

        // Target of composite foreign key from Employees
        builder.HasAlternateKey(g => new { g.GradeId, g.DesignationId })
            .HasName("UQ_Grades_Id_Designation");

        // Foreign key to Designation
        builder.HasOne(g => g.Designation)
            .WithMany(desig => desig.Grades)
            .HasForeignKey(g => g.DesignationId)
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Grades_Designations");
    }
}
