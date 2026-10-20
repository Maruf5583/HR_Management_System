using Hrms.Audit.Application.Services;
using Hrms.Audit.Contracts;
using Hrms.Audit.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();

        return services;
    }
}
