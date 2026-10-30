using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Reports;

public static class ReportsModule
{
    public static IServiceCollection AddReportsModule(this IServiceCollection services)
    {
        services.AddScoped<IReportsQueryService, ReportsQueryService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();

        return services;
    }
}
