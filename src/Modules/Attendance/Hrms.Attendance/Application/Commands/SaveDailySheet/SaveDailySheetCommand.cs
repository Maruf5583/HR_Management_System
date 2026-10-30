using FluentValidation;
using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Domain.Entities;
using Hrms.Attendance.Domain.Services;
using Hrms.Calendar.Contracts;
using Hrms.Leave.Contracts;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SaveDailySheet;

public record SaveDailySheetCommand(
    DateOnly WorkDate,
    IReadOnlyList<DailySheetItemInput> Items
);

public class SaveDailySheetCommandValidator : AbstractValidator<SaveDailySheetCommand>
{
    public SaveDailySheetCommandValidator()
    {
        RuleFor(x => x.WorkDate).NotEmpty();
        RuleFor(x => x.Items).NotNull();
    }
}

public interface ISaveDailySheetHandler
{
    Task<Result> HandleAsync(SaveDailySheetCommand command, CancellationToken cancellationToken = default);
}

public class SaveDailySheetHandler : ISaveDailySheetHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly ILeaveQuery _leaveQuery;
    private readonly ISettingsReader _settingsReader;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public SaveDailySheetHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        ILeaveQuery leaveQuery,
        ISettingsReader settingsReader,
        IClock clock,
        ICurrentUser currentUser)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _leaveQuery = leaveQuery;
        _settingsReader = settingsReader;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<Result> HandleAsync(SaveDailySheetCommand command, CancellationToken cancellationToken = default)
    {
        var workDate = command.WorkDate;
        var now = _clock.UtcNow;
        var userId = _currentUser.UserId;

        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var holidayPriority = await _settingsReader.GetStringAsync(SettingKeys.AttendanceHolidayPriority, cancellationToken) ?? "Holiday";
        var halfDayHours = await _settingsReader.GetDecimalAsync(SettingKeys.AttendanceHalfDayHours, cancellationToken);

        var isHoliday = await _calendarQuery.IsHolidayAsync(workDate, cancellationToken);
        var isWorkingDay = await _calendarQuery.IsWorkingDayAsync(workDate, cancellationToken);
        var isWeeklyOff = !isWorkingDay;

        var existingRecords = await _context.AttendanceDays
            .Where(a => a.WorkDate == workDate)
            .ToDictionaryAsync(a => a.EmployeeId, cancellationToken);

        foreach (var item in command.Items)
        {
            var shift = await _calendarQuery.GetShiftForEmployeeAsync(item.EmployeeId, workDate, cancellationToken);
            var isOnLeave = await _leaveQuery.IsOnLeaveAsync(item.EmployeeId, workDate, cancellationToken);

            DateTime? inUtc = null;
            DateTime? outUtc = null;

            if (item.CheckInTime.HasValue)
            {
                var checkInLocal = workDate.ToDateTime(item.CheckInTime.Value);
                inUtc = TimeZoneInfo.ConvertTimeToUtc(checkInLocal, timeZone);

                if (item.CheckOutTime.HasValue)
                {
                    var checkOutLocal = workDate.ToDateTime(item.CheckOutTime.Value);
                    if (checkOutLocal < checkInLocal)
                    {
                        checkOutLocal = checkOutLocal.AddDays(1);
                    }
                    outUtc = TimeZoneInfo.ConvertTimeToUtc(checkOutLocal, timeZone);
                }
            }

            var calc = AttendanceStatusCalculator.Calculate(
                workDate: workDate,
                isHoliday: isHoliday,
                isWeeklyOff: isWeeklyOff,
                holidayPriority: holidayPriority,
                isOnLeave: isOnLeave,
                checkInUtc: inUtc,
                checkOutUtc: outUtc,
                candidateShiftVersionId: shift?.ShiftVersionId,
                shiftStartTime: shift?.StartTime,
                graceMinutes: shift?.GraceMinutes ?? 0,
                breakMinutes: shift?.BreakMinutes ?? 0,
                halfDayHours: halfDayHours,
                timeZone: timeZone
            );

            if (existingRecords.TryGetValue(item.EmployeeId, out var record))
            {
                record.ShiftVersionId = calc.ShiftVersionId;
                record.CheckInUtc = inUtc;
                record.CheckOutUtc = outUtc;
                record.LateMinutes = calc.LateMinutes;
                record.WorkedMinutes = calc.WorkedMinutes;
                record.Status = calc.Status;
                if (!string.IsNullOrWhiteSpace(item.Note))
                {
                    record.Note = item.Note.Trim();
                }
                record.UpdatedAtUtc = now;
                record.UpdatedByUserId = userId;
            }
            else
            {
                var newRecord = new AttendanceDay
                {
                    AttendanceDayId = Guid.NewGuid(),
                    EmployeeId = item.EmployeeId,
                    WorkDate = workDate,
                    ShiftVersionId = calc.ShiftVersionId,
                    CheckInUtc = inUtc,
                    CheckOutUtc = outUtc,
                    LateMinutes = calc.LateMinutes,
                    WorkedMinutes = calc.WorkedMinutes,
                    Status = calc.Status,
                    Note = string.IsNullOrWhiteSpace(item.Note) ? null : item.Note.Trim(),
                    CreatedAtUtc = now,
                    CreatedByUserId = userId
                };
                await _context.AttendanceDays.AddAsync(newRecord, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
