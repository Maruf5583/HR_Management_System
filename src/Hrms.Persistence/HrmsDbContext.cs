using Hrms.Assets.Application.Abstractions;
using Hrms.Assets.Domain.Entities;
using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Hrms.Audit.Application.Abstractions;
using Hrms.Audit.Domain.Entities;
using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Hrms.Identity.Domain.Entities;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
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
    ICalendarDbContext,
    IAssetsDbContext,
    ILeaveDbContext,
    IAttendanceDbContext
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

    // Assets Module
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetAssignment> AssetAssignments => Set<AssetAssignment>();

    // Leave Module
    public DbSet<LeaveType> LeaveTypes => Set<LeaveType>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();

    // Attendance Module
    public DbSet<AttendanceDay> AttendanceDays => Set<AttendanceDay>();

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

        // Cross-module relation: AssetAssignments.EmployeeId -> Employees.EmployeeId
        builder.Entity<AssetAssignment>(b =>
        {
            b.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AssetAssignments_Employees");
        });

        // Cross-module relation: LeaveBalances.EmployeeId -> Employees.EmployeeId
        builder.Entity<LeaveBalance>(b =>
        {
            b.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(lb => lb.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_LeaveBalances_Employees");
        });

        // Cross-module relation: LeaveRecords.EmployeeId -> Employees.EmployeeId
        builder.Entity<LeaveRecord>(b =>
        {
            b.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(lr => lr.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_LeaveRecords_Employees");
        });

        // Cross-module relations: AttendanceDays -> Employees, ShiftVersions
        builder.Entity<AttendanceDay>(b =>
        {
            b.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AttendanceDays_Employees");

            b.HasOne<ShiftVersion>()
                .WithMany()
                .HasForeignKey(a => a.ShiftVersionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_AttendanceDays_ShiftVersions");
        });
    }
}
