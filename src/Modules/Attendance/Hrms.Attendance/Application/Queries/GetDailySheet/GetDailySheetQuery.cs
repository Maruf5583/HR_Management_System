using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Domain.Enums;
using Hrms.Attendance.Domain.Services;
using Hrms.Calendar.Contracts;
using Hrms.Employees.Contracts;
using Hrms.Leave.Contracts;
using Hrms.Settings.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Queries.GetDailySheet;

public record GetDailySheetQuery(DateOnly WorkDate);

public interface IGetDailySheetHandler
{
    Task<DailySheetDto> HandleAsync(GetDailySheetQuery query, CancellationToken cancellationToken = default);
}

public class GetDailySheetHandler : IGetDailySheetHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ICalendarQuery _calendarQuery;
    private readonly ILeaveQuery _leaveQuery;
    private readonly ISettingsReader _settingsReader;

    public GetDailySheetHandler(
        IAttendanceDbContext context,
        IEmployeeLookup employeeLookup,
        ICalendarQuery calendarQuery,
        ILeaveQuery leaveQuery,
        ISettingsReader settingsReader)
    {
        _context = context;
        _employeeLookup = employeeLookup;
        _calendarQuery = calendarQuery;
        _leaveQuery = leaveQuery;
        _settingsReader = settingsReader;
    }

    public async Task<DailySheetDto> HandleAsync(GetDailySheetQuery query, CancellationToken cancellationToken = default)
    {
        var workDate = query.WorkDate;
        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var holidayPriority = await _settingsReader.GetStringAsync(SettingKeys.AttendanceHolidayPriority, cancellationToken) ?? "Holiday";
        var halfDayHours = await _settingsReader.GetDecimalAsync(SettingKeys.AttendanceHalfDayHours, cancellationToken);

        var isHoliday = await _calendarQuery.IsHolidayAsync(workDate, cancellationToken);
        var holiday = isHoliday ? await _calendarQuery.GetHolidayAsync(workDate, cancellationToken) : null;
        var isWorkingDay = await _calendarQuery.IsWorkingDayAsync(workDate, cancellationToken);
        var isWeeklyOff = !isWorkingDay;

        var employees = await _employeeLookup.GetActiveEmployeesDropdownAsync(cancellationToken);
        var existingRecords = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.WorkDate == workDate)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        var items = new List<DailySheetItemDto>();

        foreach (var emp in employees.OrderBy(e => e.EmployeeCode))
        {
            var shift = await _calendarQuery.GetShiftForEmployeeAsync(emp.EmployeeId, workDate, cancellationToken);
            var isOnLeave = await _leaveQuery.IsOnLeaveAsync(emp.EmployeeId, workDate, cancellationToken);

            if (existingRecords.TryGetValue(emp.EmployeeId, out var existing))
            {
                TimeOnly? inLocal = existing.CheckInUtc.HasValue
                    ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(existing.CheckInUtc.Value, timeZone))
                    : null;

                TimeOnly? outLocal = existing.CheckOutUtc.HasValue
                    ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(existing.CheckOutUtc.Value, timeZone))
                    : null;

                bool isReadonly = existing.Status == AttendanceStatus.Holiday ||
                                  existing.Status == AttendanceStatus.Weekend ||
                                  existing.Status == AttendanceStatus.OnLeave;

                items.Add(new DailySheetItemDto(
                    EmployeeId: emp.EmployeeId,
                    EmployeeCode: emp.EmployeeCode,
                    FullName: emp.FullName,
                    AttendanceDayId: existing.AttendanceDayId,
                    CheckInTime: inLocal,
                    CheckOutTime: outLocal,
                    LateMinutes: existing.LateMinutes,
                    WorkedMinutes: existing.WorkedMinutes,
                    Status: existing.Status,
                    Note: existing.Note,
                    ShiftName: shift?.ShiftName,
                    ShiftStartTime: shift?.StartTime,
                    ShiftEndTime: shift?.EndTime,
                    ShiftVersionId: existing.ShiftVersionId,
                    IsReadonly: isReadonly,
                    RowVersion: existing.RowVersion
                ));
            }
            else
            {
                // Pre-calculate initial status
                var initialCalc = AttendanceStatusCalculator.Calculate(
                    workDate: workDate,
                    isHoliday: isHoliday,
                    isWeeklyOff: isWeeklyOff,
                    holidayPriority: holidayPriority,
                    isOnLeave: isOnLeave,
                    checkInUtc: null,
                    checkOutUtc: null,
                    candidateShiftVersionId: shift?.ShiftVersionId,
                    shiftStartTime: shift?.StartTime,
                    graceMinutes: shift?.GraceMinutes ?? 0,
                    breakMinutes: shift?.BreakMinutes ?? 0,
                    halfDayHours: halfDayHours,
                    timeZone: timeZone
                );

                bool isReadonly = initialCalc.Status == AttendanceStatus.Holiday ||
                                  initialCalc.Status == AttendanceStatus.Weekend ||
                                  initialCalc.Status == AttendanceStatus.OnLeave;

                items.Add(new DailySheetItemDto(
                    EmployeeId: emp.EmployeeId,
                    EmployeeCode: emp.EmployeeCode,
                    FullName: emp.FullName,
                    AttendanceDayId: null,
                    CheckInTime: null,
                    CheckOutTime: null,
                    LateMinutes: initialCalc.LateMinutes,
                    WorkedMinutes: initialCalc.WorkedMinutes,
                    Status: initialCalc.Status,
                    Note: null,
                    ShiftName: shift?.ShiftName,
                    ShiftStartTime: shift?.StartTime,
                    ShiftEndTime: shift?.EndTime,
                    ShiftVersionId: initialCalc.ShiftVersionId,
                    IsReadonly: isReadonly,
                    RowVersion: null
                ));
            }
        }

        return new DailySheetDto(
            WorkDate: workDate,
            IsHoliday: isHoliday,
            HolidayName: holiday?.Name,
            IsWeeklyOff: isWeeklyOff,
            Items: items
        );
    }
}
