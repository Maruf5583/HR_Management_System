using Hrms.Audit.Application.Abstractions;
using Hrms.Audit.Domain.Entities;
using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Hrms.Identity.Domain.Entities;
using Hrms.Persistence.Interceptors;
using Hrms.Settings.Application.Abstractions;
using Hrms.Settings.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Persistence;

public class HrmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>,
    IEmployeesDbContext,
    ISettingsDbContext,
    IAuditDbContext,
    ICalendarDbContext
{
    private readonly AuditableEntityInterceptor? _auditableEntityInterceptor;
    private readonly AuditLogInterceptor? _auditLogInterceptor;

    public HrmsDbContext(
        DbContextOptions<HrmsDbContext> options,
        AuditableEntityInterceptor? auditableEntityInterceptor = null,
        AuditLogInterceptor? auditLogInterceptor = null)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
        _auditLogInterceptor = auditLogInterceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditableEntityInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        if (_auditLogInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditLogInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Setting> Settings => Set<Setting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Calendar Module
    public DbSet<CompanyWorkingDay> CompanyWorkingDays => Set<CompanyWorkingDay>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<ShiftVersion> ShiftVersions => Set<ShiftVersion>();
    public DbSet<ShiftAssignment> ShiftAssignments => Set<ShiftAssignment>();
    public DbSet<Holiday> Holidays => Set<Holiday>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(HrmsDbContext).Assembly);

        // Cross-module relation: AspNetUsers.EmployeeId -> Employees.EmployeeId
        builder.Entity<ApplicationUser>(b =>
        {
            b.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
