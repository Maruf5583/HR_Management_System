using Hrms.Attendance.Application.Abstractions;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record EndBreakCommand(Guid EmployeeId);

public interface IEndBreakHandler
{
    Task<Result> HandleAsync(EndBreakCommand command, CancellationToken cancellationToken = default);
}

public class EndBreakHandler : IEndBreakHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IClock _clock;

    public EndBreakHandler(IAttendanceDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(EndBreakCommand command, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var day = await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.EmployeeId == command.EmployeeId && a.WorkDate == today, cancellationToken);

        if (day == null)
        {
            return Result.Failure(Error.Validation("No attendance record found for today."));
        }

        var activeBreak = await _context.AttendanceBreaks
            .FirstOrDefaultAsync(b => b.AttendanceDayId == day.AttendanceDayId && b.BreakEndUtc == null, cancellationToken);

        if (activeBreak == null)
        {
            return Result.Failure(Error.NotFound("You do not have an active break to end."));
        }

        var nowUtc = _clock.UtcNow;
        activeBreak.BreakEndUtc = nowUtc;
        activeBreak.Minutes = (int)Math.Max(0, (nowUtc - activeBreak.BreakStartUtc).TotalMinutes);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
