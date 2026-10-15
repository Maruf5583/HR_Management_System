using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Hrms.Identity.Domain.Entities;
using Hrms.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Persistence;

public class HrmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IEmployeesDbContext
{
    private readonly AuditableEntityInterceptor? _auditableEntityInterceptor;

    public HrmsDbContext(
        DbContextOptions<HrmsDbContext> options,
        AuditableEntityInterceptor? auditableEntityInterceptor = null)
        : base(options)
    {
        _auditableEntityInterceptor = auditableEntityInterceptor;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (_auditableEntityInterceptor != null)
        {
            optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
        }

        base.OnConfiguring(optionsBuilder);
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Designation> Designations => Set<Designation>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<Employee> Employees => Set<Employee>();

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
             .OnDelete(DeleteBehavior.NoAction)
             .HasConstraintName("FK_AspNetUsers_Employees");

            b.HasIndex(u => u.EmployeeId)
             .IsUnique()
             .HasFilter("[EmployeeId] IS NOT NULL")
             .HasDatabaseName("UX_AspNetUsers_EmployeeId");
        });
    }
}
