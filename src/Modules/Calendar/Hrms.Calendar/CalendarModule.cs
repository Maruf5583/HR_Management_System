using FluentValidation;
using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Contracts;
using Hrms.Calendar.Infrastructure.Repositories;
using Hrms.Calendar.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Calendar;

public static class CalendarModule
{
    public static IServiceCollection AddCalendarModule(this IServiceCollection services)
    {
        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(CalendarModule).Assembly);

        // Repositories
        services.AddScoped<ICompanyWorkingDayRepository, CompanyWorkingDayRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();
        services.AddScoped<IHolidayRepository, HolidayRepository>();

        // Domain Services
        services.AddScoped<IWorkingDayService, WorkingDayService>();
        services.AddScoped<IShiftService, ShiftService>();
        services.AddScoped<IHolidayService, HolidayService>();

        // Cross-Module Contracts
        services.AddScoped<ICalendarQuery, CalendarQueryService>();

        return services;
    }
}
