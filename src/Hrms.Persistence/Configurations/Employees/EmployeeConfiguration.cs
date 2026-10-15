using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hrms.Persistence.Configurations.Employees;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", t =>
        {
            t.HasCheckConstraint("CK_Employees_EmploymentType", "[EmploymentType] IN ('Permanent','Probation','Contract','Intern')");
            t.HasCheckConstraint("CK_Employees_Status", "[Status] IN ('Active','OnNotice','Resigned','Terminated')");
            t.HasCheckConstraint("CK_Employees_ExitDate", "[ExitDate] IS NULL OR [ExitDate] >= [JoinDate]");
        });

        builder.HasKey(e => e.EmployeeId);

        builder.Property(e => e.EmployeeId)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        builder.Property(e => e.EmployeeCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("UQ_Employees_Code");

        builder.Property(e => e.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasMaxLength(256);

        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL")
            .HasDatabaseName("UX_Employees_Email");

        builder.Property(e => e.Phone)
            .HasMaxLength(30);

        builder.Property(e => e.DateOfBirth)
            .HasColumnType("date");

        builder.Property(e => e.Gender)
            .HasMaxLength(20);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.NidNumberEncrypted)
            .HasMaxLength(500);

        builder.Property(e => e.PhotoKey)
            .HasMaxLength(260);

        builder.Property(e => e.JoinDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(e => e.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.EmergencyContactName)
            .HasMaxLength(150);

        builder.Property(e => e.EmergencyContactPhone)
            .HasMaxLength(30);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValueSql("'Active'")
            .IsRequired();

        builder.Property(e => e.ExitDate)
            .HasColumnType("date");

        builder.Property(e => e.CreatedAtUtc)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .HasColumnType("datetime2(3)");

        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Department is linked through DesignationId and DepartmentId composite key
        builder.Ignore(e => e.Department);

        // Composite Foreign Keys
        builder.HasOne(e => e.Designation)
            .WithMany()
            .HasForeignKey(e => new { e.DesignationId, e.DepartmentId })
            .HasPrincipalKey(d => new { d.DesignationId, d.DepartmentId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Employees_Designation_Dept");

        builder.HasOne(e => e.Grade)
            .WithMany()
            .HasForeignKey(e => new { e.GradeId, e.DesignationId })
            .HasPrincipalKey(g => new { g.GradeId, g.DesignationId })
            .OnDelete(DeleteBehavior.NoAction)
            .HasConstraintName("FK_Employees_Grade_Designation");

        // Indexes defined in docs/hrms_v1_schema.sql
        builder.HasIndex(e => new { e.DepartmentId, e.Status })
            .HasDatabaseName("IX_Employees_Dept_Status");

        builder.HasIndex(e => e.GradeId)
            .HasDatabaseName("IX_Employees_Grade");
    }
}
