using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record ClockOutCommand(Guid EmployeeId);

public interface IClockOutHandler
{
    Task<Result> HandleAsync(ClockOutCommand command, CancellationToken cancellationToken = default);
}

public class ClockOutHandler : IClockOutHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IClock _clock;

    public ClockOutHandler(IAttendanceDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ClockOutCommand command, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var day = await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.EmployeeId == command.EmployeeId && a.WorkDate == today, cancellationToken);

        if (day == null || day.CheckInUtc == null)
        {
            return Result.Failure(Error.Validation("You must start work before you can clock out."));
        }

        if (day.CheckOutUtc != null)
        {
            return Result.Failure(Error.Conflict("You have already clocked out for today."));
        }

        var nowUtc = _clock.UtcNow;

        // Auto-end active break if any
        var activeBreak = await _context.AttendanceBreaks
            .FirstOrDefaultAsync(b => b.AttendanceDayId == day.AttendanceDayId && b.BreakEndUtc == null, cancellationToken);

        if (activeBreak != null)
        {
            activeBreak.BreakEndUtc = nowUtc;
            activeBreak.Minutes = (int)Math.Max(0, (nowUtc - activeBreak.BreakStartUtc).TotalMinutes);
        }

        day.CheckOutUtc = nowUtc;

        var breaks = await _context.AttendanceBreaks
            .Where(b => b.AttendanceDayId == day.AttendanceDayId)
            .ToListAsync(cancellationToken);

        int totalBreakMinutes = breaks.Sum(b => b.Minutes);
        int totalElapsed = (int)Math.Max(0, (day.CheckOutUtc.Value - day.CheckInUtc.Value).TotalMinutes);
        int workedMinutes = Math.Max(0, totalElapsed - totalBreakMinutes);

        day.WorkedMinutes = workedMinutes;

        // If worked less than 4 hours (240 minutes), set to HalfDay if not late
        if (workedMinutes < 240 && day.Status == AttendanceStatus.Present)
        {
            day.Status = AttendanceStatus.HalfDay;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
