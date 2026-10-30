using FluentValidation;
using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Application.Commands.SaveDailySheet;
using Hrms.Attendance.Application.Commands.UpdateAttendanceRecord;
using Hrms.Attendance.Application.Queries.GetDailySheet;
using Hrms.Attendance.Application.Queries.GetEmployeeAttendance;
using Hrms.Attendance.Application.Queries.GetMonthlyAttendance;
using Hrms.Attendance.Contracts;
using Hrms.Attendance.Infrastructure.Repositories;
using Hrms.Attendance.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrms.Attendance;

public static class AttendanceModule
{
    public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
    {
        // Repositories & Services
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IAttendanceMarker, AttendanceMarker>();
        services.AddScoped<IAttendanceQuery, AttendanceQueryService>();

        // Query Handlers
        services.AddScoped<IGetDailySheetHandler, GetDailySheetHandler>();
        services.AddScoped<IGetMonthlyAttendanceHandler, GetMonthlyAttendanceHandler>();
        services.AddScoped<IGetEmployeeAttendanceHandler, GetEmployeeAttendanceHandler>();

        // Command Handlers
        services.AddScoped<ISaveDailySheetHandler, SaveDailySheetHandler>();
        services.AddScoped<IUpdateAttendanceRecordHandler, UpdateAttendanceRecordHandler>();

        // Validators
        services.AddValidatorsFromAssemblyContaining<SaveDailySheetCommandValidator>();

        return services;
    }
}
