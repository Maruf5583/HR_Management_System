using Hrms.Identity.Domain.Entities;
using Hrms.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Persistence;

public class HrmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(HrmsDbContext).Assembly);
    }
}
