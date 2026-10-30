using System.Text.Json;
using FluentValidation;
using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Services;
using Hrms.Audit.Contracts;
using Hrms.Audit.Contracts.Models;
using Hrms.Calendar.Contracts;
using Hrms.Leave.Contracts;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.UpdateAttendanceRecord;

public record UpdateAttendanceRecordCommand(
    Guid AttendanceDayId,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    string Note,
    byte[] RowVersion
);

public class UpdateAttendanceRecordCommandValidator : AbstractValidator<UpdateAttendanceRecordCommand>
{
    public UpdateAttendanceRecordCommandValidator()
    {
        RuleFor(x => x.AttendanceDayId).NotEmpty();
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("A note is required when editing an attendance record.")
            .MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
    }
}

public interface IUpdateAttendanceRecordHandler
{
    Task<Result> HandleAsync(UpdateAttendanceRecordCommand command, CancellationToken cancellationToken = default);
}

public class UpdateAttendanceRecordHandler : IUpdateAttendanceRecordHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly ILeaveQuery _leaveQuery;
    private readonly ISettingsReader _settingsReader;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditWriter _auditWriter;

    public UpdateAttendanceRecordHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        ILeaveQuery leaveQuery,
        ISettingsReader settingsReader,
        IClock clock,
        ICurrentUser currentUser,
        IAuditWriter auditWriter)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _leaveQuery = leaveQuery;
        _settingsReader = settingsReader;
        _clock = clock;
        _currentUser = currentUser;
        _auditWriter = auditWriter;
    }

    public async Task<Result> HandleAsync(UpdateAttendanceRecordCommand command, CancellationToken cancellationToken = default)
    {
        var record = await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.AttendanceDayId == command.AttendanceDayId, cancellationToken);

        if (record == null)
        {
            return Result.Failure(Error.NotFound("Attendance record was not found."));
        }

        // Concurrency check
        _context.AttendanceDays.Entry(record).Property(nameof(record.RowVersion)).OriginalValue = command.RowVersion;

        var workDate = record.WorkDate;
        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var holidayPriority = await _settingsReader.GetStringAsync(SettingKeys.AttendanceHolidayPriority, cancellationToken) ?? "Holiday";
        var halfDayHours = await _settingsReader.GetDecimalAsync(SettingKeys.AttendanceHalfDayHours, cancellationToken);

        var isHoliday = await _calendarQuery.IsHolidayAsync(workDate, cancellationToken);
        var isWorkingDay = await _calendarQuery.IsWorkingDayAsync(workDate, cancellationToken);
        var isWeeklyOff = !isWorkingDay;

        var shift = await _calendarQuery.GetShiftForEmployeeAsync(record.EmployeeId, workDate, cancellationToken);
        var isOnLeave = await _leaveQuery.IsOnLeaveAsync(record.EmployeeId, workDate, cancellationToken);

        DateTime? inUtc = null;
        DateTime? outUtc = null;

        if (command.CheckInTime.HasValue)
        {
            var checkInLocal = workDate.ToDateTime(command.CheckInTime.Value);
            inUtc = TimeZoneInfo.ConvertTimeToUtc(checkInLocal, timeZone);

            if (command.CheckOutTime.HasValue)
            {
                var checkOutLocal = workDate.ToDateTime(command.CheckOutTime.Value);
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

        var oldValues = JsonSerializer.Serialize(new
        {
            record.CheckInUtc,
            record.CheckOutUtc,
            record.LateMinutes,
            record.WorkedMinutes,
            record.Status,
            record.Note
        });

        record.ShiftVersionId = calc.ShiftVersionId;
        record.CheckInUtc = inUtc;
        record.CheckOutUtc = outUtc;
        record.LateMinutes = calc.LateMinutes;
        record.WorkedMinutes = calc.WorkedMinutes;
        record.Status = calc.Status;
        record.Note = command.Note.Trim();
        record.UpdatedAtUtc = _clock.UtcNow;
        record.UpdatedByUserId = _currentUser.UserId;

        var newValues = JsonSerializer.Serialize(new
        {
            record.CheckInUtc,
            record.CheckOutUtc,
            record.LateMinutes,
            record.WorkedMinutes,
            record.Status,
            record.Note
        });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure(Error.Conflict("Someone else changed this record, reload and try again."));
        }

        // Audit log
        await _auditWriter.WriteAsync(new AuditLogEntry(
            UserId: _currentUser.UserId,
            Action: "Update",
            EntityName: "AttendanceDays",
            EntityId: record.AttendanceDayId.ToString(),
            OldValues: oldValues,
            NewValues: newValues,
            CreatedAtUtc: _clock.UtcNow
        ), cancellationToken);

        return Result.Success();
    }
}
