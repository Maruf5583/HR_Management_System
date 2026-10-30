using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Domain.Enums;
using Hrms.Calendar.Contracts;
using Hrms.Settings.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Queries.GetEmployeeAttendance;

public record GetEmployeeAttendanceQuery(
    Guid EmployeeId,
    int Year,
    int Month
);

public interface IGetEmployeeAttendanceHandler
{
    Task<EmployeeMonthlyReportDto> HandleAsync(GetEmployeeAttendanceQuery query, CancellationToken cancellationToken = default);
}

public class GetEmployeeAttendanceHandler : IGetEmployeeAttendanceHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly ISettingsReader _settingsReader;

    public GetEmployeeAttendanceHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        ISettingsReader settingsReader)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _settingsReader = settingsReader;
    }

    public async Task<EmployeeMonthlyReportDto> HandleAsync(GetEmployeeAttendanceQuery query, CancellationToken cancellationToken = default)
    {
        var year = query.Year;
        var month = query.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = new DateOnly(year, month, daysInMonth);

        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var records = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.EmployeeId == query.EmployeeId && a.WorkDate >= firstDay && a.WorkDate <= lastDay)
            .ToDictionaryAsync(a => a.WorkDate, cancellationToken);

        var dayDtos = new List<EmployeeAttendanceDayDto>();
        int present = 0, late = 0, halfDay = 0, absent = 0, onLeave = 0, holiday = 0, weekend = 0;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            var shift = await _calendarQuery.GetShiftForEmployeeAsync(query.EmployeeId, date, cancellationToken);

            if (records.TryGetValue(date, out var record))
            {
                TimeOnly? inLocal = record.CheckInUtc.HasValue
                    ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(record.CheckInUtc.Value, timeZone))
                    : null;

                TimeOnly? outLocal = record.CheckOutUtc.HasValue
                    ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(record.CheckOutUtc.Value, timeZone))
                    : null;

                switch (record.Status)
                {
                    case AttendanceStatus.Present: present++; break;
                    case AttendanceStatus.Late: late++; break;
                    case AttendanceStatus.HalfDay: halfDay++; break;
                    case AttendanceStatus.Absent: absent++; break;
                    case AttendanceStatus.OnLeave: onLeave++; break;
                    case AttendanceStatus.Holiday: holiday++; break;
                    case AttendanceStatus.Weekend: weekend++; break;
                }

                dayDtos.Add(new EmployeeAttendanceDayDto(
                    WorkDate: date,
                    DayOfWeek: date.DayOfWeek.ToString(),
                    CheckInTime: inLocal,
                    CheckOutTime: outLocal,
                    LateMinutes: record.LateMinutes,
                    WorkedMinutes: record.WorkedMinutes,
                    Status: record.Status,
                    Note: record.Note,
                    ShiftName: shift?.ShiftName
                ));
            }
            else
            {
                dayDtos.Add(new EmployeeAttendanceDayDto(
                    WorkDate: date,
                    DayOfWeek: date.DayOfWeek.ToString(),
                    CheckInTime: null,
                    CheckOutTime: null,
                    LateMinutes: 0,
                    WorkedMinutes: 0,
                    Status: AttendanceStatus.Absent,
                    Note: null,
                    ShiftName: shift?.ShiftName
                ));
            }
        }

        return new EmployeeMonthlyReportDto(
            EmployeeId: query.EmployeeId,
            Year: year,
            Month: month,
            PresentDays: present,
            LateDays: late,
            HalfDays: halfDay,
            AbsentDays: absent,
            OnLeaveDays: onLeave,
            HolidayDays: holiday,
            WeekendDays: weekend,
            Days: dayDtos
        );
    }
}
