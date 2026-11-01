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
        var nowUtc = _clock.UtcNow;
        var localTime = _clock.ToLocal(nowUtc);

        int graceMinutes = shift?.GraceMinutes > 0 ? shift.GraceMinutes : 15;
        int lateMinutes = 0;
        var status = AttendanceStatus.Present;

        if (shift != null)
        {
            var shiftStart = shift.StartTime;
            var graceCutoff = shiftStart.Add(TimeSpan.FromMinutes(graceMinutes));
            var currentTimeOfDay = localTime.TimeOfDay;

            if (currentTimeOfDay > graceCutoff)
            {
                status = AttendanceStatus.Late;
                lateMinutes = (int)Math.Max(0, (currentTimeOfDay - graceCutoff).TotalMinutes);
            }
        }

        if (existing == null)
        {
            existing = new AttendanceDay
            {
                AttendanceDayId = Guid.NewGuid(),
                EmployeeId = command.EmployeeId,
                WorkDate = today,
                ShiftVersionId = shift?.ShiftVersionId,
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
            existing.ShiftVersionId = shift?.ShiftVersionId;
            existing.Status = status;
            existing.LateMinutes = lateMinutes;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
