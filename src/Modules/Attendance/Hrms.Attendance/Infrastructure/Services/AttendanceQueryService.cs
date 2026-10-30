using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Contracts;
using Hrms.Attendance.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Infrastructure.Services;

public class AttendanceQueryService : IAttendanceQuery
{
    private readonly IAttendanceDbContext _context;

    public AttendanceQueryService(IAttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceMonthSummaryDto> GetMonthlySummaryAsync(
        Guid employeeId,
        DateOnly monthStart,
        CancellationToken cancellationToken = default)
    {
        var firstDay = new DateOnly(monthStart.Year, monthStart.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);

        var days = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= firstDay && a.WorkDate <= lastDay)
            .ToListAsync(cancellationToken);

        int present = days.Count(d => d.Status == AttendanceStatus.Present);
        int absent = days.Count(d => d.Status == AttendanceStatus.Absent);
        int halfDays = days.Count(d => d.Status == AttendanceStatus.HalfDay);
        int late = days.Count(d => d.Status == AttendanceStatus.Late);
        int onLeave = days.Count(d => d.Status == AttendanceStatus.OnLeave);
        int holiday = days.Count(d => d.Status == AttendanceStatus.Holiday);
        int weekend = days.Count(d => d.Status == AttendanceStatus.Weekend);

        return new AttendanceMonthSummaryDto(
            employeeId,
            present,
            absent,
            halfDays,
            late,
            onLeave,
            holiday,
            weekend
        );
    }
}
