using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record StartBreakCommand(Guid EmployeeId);

public interface IStartBreakHandler
{
    Task<Result> HandleAsync(StartBreakCommand command, CancellationToken cancellationToken = default);
}

public class StartBreakHandler : IStartBreakHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IClock _clock;

    public StartBreakHandler(IAttendanceDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(StartBreakCommand command, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var day = await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.EmployeeId == command.EmployeeId && a.WorkDate == today, cancellationToken);

        if (day == null || day.CheckInUtc == null)
        {
            return Result.Failure(Error.Validation("You must start work before taking a break."));
        }

        if (day.CheckOutUtc != null)
        {
            return Result.Failure(Error.Validation("You have already clocked out for today."));
        }

        var hasActiveBreak = await _context.AttendanceBreaks
            .AnyAsync(b => b.AttendanceDayId == day.AttendanceDayId && b.BreakEndUtc == null, cancellationToken);

        if (hasActiveBreak)
        {
            return Result.Failure(Error.Conflict("You already have an active break in progress."));
        }

        var newBreak = new AttendanceBreak
        {
            AttendanceBreakId = Guid.NewGuid(),
            AttendanceDayId = day.AttendanceDayId,
            BreakStartUtc = _clock.UtcNow,
            Minutes = 0
        };

        _context.AttendanceBreaks.Add(newBreak);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
