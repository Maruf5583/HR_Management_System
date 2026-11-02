using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Hrms.Attendance.Domain.Enums;
using Hrms.Calendar.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record ClockInCommand(Guid EmployeeId);

public interface IClockInHandler
{
    Task<Result> HandleAsync(ClockInCommand command, CancellationToken cancellationToken = default);
}

public class ClockInHandler : IClockInHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly IClock _clock;

    public ClockInHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        IClock clock)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ClockInCommand command, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var existing = await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.EmployeeId == command.EmployeeId && a.WorkDate == today, cancellationToken);

        if (existing?.CheckInUtc != null)
        {
            return Result.Failure(Error.Conflict("You have already started work today."));
        }

        var shift = await _calendarQuery.GetShiftForEmployeeAsync(command.EmployeeId, today, cancellationToken);
        if (shift == null)
        {
            return Result.Failure(Error.Validation("No shift is scheduled for you today. You cannot start work."));
        }

        var nowUtc = _clock.UtcNow;
        var localTime = _clock.ToLocal(nowUtc);

        var shiftStart = TimeOnly.FromTimeSpan(shift.StartTime);
        var shiftEnd = TimeOnly.FromTimeSpan(shift.EndTime);

        DateTime shiftStartDateTime = today.ToDateTime(shiftStart);
        DateTime shiftEndDateTime;

        if (shift.IsNightShift || shiftEnd < shiftStart)
        {
            shiftEndDateTime = today.AddDays(1).ToDateTime(shiftEnd);
        }
        else
        {
            shiftEndDateTime = today.ToDateTime(shiftEnd);
        }

        // Allowed clock-in window: opens 30 minutes before shift start, closes at shift end
        var earliestAllowed = shiftStartDateTime.AddMinutes(-30);

        if (localTime < earliestAllowed)
        {
            return Result.Failure(Error.Validation(
                $"You cannot start work outside your scheduled shift hours. Your shift ({shift.ShiftName}) starts at {shiftStart:hh\\:mm\\ tt}. Clock-in opens at {earliestAllowed:hh\\:mm\\ tt}."));
        }

        if (localTime > shiftEndDateTime)
        {
            return Result.Failure(Error.Validation(
                $"Your scheduled shift ({shift.ShiftName}) ended at {shiftEnd:hh\\:mm\\ tt}. You cannot start work after shift hours. Please submit an attendance correction request if you worked today."));
        }

        int graceMinutes = shift.GraceMinutes > 0 ? shift.GraceMinutes : 15;
        int lateMinutes = 0;
        var status = AttendanceStatus.Present;

        var graceCutoff = shiftStartDateTime.AddMinutes(graceMinutes);
        if (localTime > graceCutoff)
        {
            status = AttendanceStatus.Late;
            lateMinutes = (int)Math.Max(0, (localTime - graceCutoff).TotalMinutes);
        }

        if (existing == null)
        {
            existing = new AttendanceDay
            {
                AttendanceDayId = Guid.NewGuid(),
                EmployeeId = command.EmployeeId,
                WorkDate = today,
                ShiftVersionId = shift.ShiftVersionId,
                CheckInUtc = nowUtc,
                Status = status,
                LateMinutes = lateMinutes,
                WorkedMinutes = 0
            };
            _context.AttendanceDays.Add(existing);
        }
        else
        {
            existing.CheckInUtc = nowUtc;
            existing.ShiftVersionId = shift.ShiftVersionId;
            existing.Status = status;
            existing.LateMinutes = lateMinutes;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
