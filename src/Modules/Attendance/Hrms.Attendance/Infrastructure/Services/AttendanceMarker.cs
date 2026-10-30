using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Contracts;
using Hrms.Attendance.Domain.Entities;
using Hrms.Attendance.Domain.Enums;
using Hrms.Attendance.Domain.Services;
using Hrms.Calendar.Contracts;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;

namespace Hrms.Attendance.Infrastructure.Services;

public class AttendanceMarker : IAttendanceMarker
{
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ICalendarQuery _calendarQuery;
    private readonly ISettingsReader _settingsReader;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public AttendanceMarker(
        IAttendanceRepository attendanceRepository,
        ICalendarQuery calendarQuery,
        ISettingsReader settingsReader,
        IClock clock,
        ICurrentUser currentUser)
    {
        _attendanceRepository = attendanceRepository;
        _calendarQuery = calendarQuery;
        _settingsReader = settingsReader;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task MarkOnLeaveAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var isHoliday = await _calendarQuery.IsHolidayAsync(date, cancellationToken);
            var isWorkingDay = await _calendarQuery.IsWorkingDayAsync(date, cancellationToken);

            // Weekends and holidays inside the leave range are excluded
            if (isHoliday || !isWorkingDay)
            {
                continue;
            }

            var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employeeId, date, cancellationToken);
            if (record != null)
            {
                record.Status = isHalfDay ? AttendanceStatus.HalfDay : AttendanceStatus.OnLeave;
                record.ShiftVersionId = null;
                record.CheckInUtc = null;
                record.CheckOutUtc = null;
                record.LateMinutes = 0;
                record.WorkedMinutes = 0;
                record.UpdatedAtUtc = now;
                record.UpdatedByUserId = userId;
                record.Note = isHalfDay ? "Half day leave" : "On approved leave";
                await _attendanceRepository.UpdateAsync(record, cancellationToken);
            }
            else
            {
                var newRecord = new AttendanceDay
                {
                    AttendanceDayId = Guid.NewGuid(),
                    EmployeeId = employeeId,
                    WorkDate = date,
                    ShiftVersionId = null,
                    CheckInUtc = null,
                    CheckOutUtc = null,
                    LateMinutes = 0,
                    WorkedMinutes = 0,
                    Status = isHalfDay ? AttendanceStatus.HalfDay : AttendanceStatus.OnLeave,
                    Note = isHalfDay ? "Half day leave" : "On approved leave",
                    CreatedAtUtc = now,
                    CreatedByUserId = userId
                };
                await _attendanceRepository.AddAsync(newRecord, cancellationToken);
            }
        }

        await _attendanceRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RecalculateDaysAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;
        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var halfDayHours = await _settingsReader.GetDecimalAsync(SettingKeys.AttendanceHalfDayHours, cancellationToken);
        var holidayPriority = await _settingsReader.GetStringAsync(SettingKeys.AttendanceHolidayPriority, cancellationToken) ?? "Holiday";

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var record = await _attendanceRepository.GetByEmployeeAndDateAsync(employeeId, date, cancellationToken);
            if (record != null && (record.Status == AttendanceStatus.OnLeave || record.Status == AttendanceStatus.HalfDay))
            {
                var isHoliday = await _calendarQuery.IsHolidayAsync(date, cancellationToken);
                var isWorkingDay = await _calendarQuery.IsWorkingDayAsync(date, cancellationToken);
                var shift = await _calendarQuery.GetShiftForEmployeeAsync(employeeId, date, cancellationToken);

                var calc = AttendanceStatusCalculator.Calculate(
                    workDate: date,
                    isHoliday: isHoliday,
                    isWeeklyOff: !isWorkingDay,
                    holidayPriority: holidayPriority,
                    isOnLeave: false,
                    checkInUtc: null,
                    checkOutUtc: null,
                    candidateShiftVersionId: shift?.ShiftVersionId,
                    shiftStartTime: shift?.StartTime,
                    graceMinutes: shift?.GraceMinutes ?? 0,
                    breakMinutes: shift?.BreakMinutes ?? 0,
                    halfDayHours: halfDayHours,
                    timeZone: timeZone
                );

                record.Status = calc.Status;
                record.ShiftVersionId = calc.ShiftVersionId;
                record.CheckInUtc = null;
                record.CheckOutUtc = null;
                record.LateMinutes = 0;
                record.WorkedMinutes = 0;
                record.Note = "Leave cancelled, recalculated";
                record.UpdatedAtUtc = now;
                record.UpdatedByUserId = userId;
                await _attendanceRepository.UpdateAsync(record, cancellationToken);
            }
        }

        await _attendanceRepository.SaveChangesAsync(cancellationToken);
    }
}
