using Hrms.Attendance.Application.Abstractions;
using Hrms.Calendar.Contracts;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Queries.GetMyAttendanceSession;

public record BreakDto(
    Guid BreakId,
    DateTime StartUtc,
    DateTime? EndUtc,
    string StartLocal,
    string? EndLocal,
    int Minutes
);

public record MyAttendanceSessionDto(
    bool HasClockedIn,
    bool IsOnBreak,
    bool HasClockedOut,
    DateTime? CheckInUtc,
    DateTime? CheckOutUtc,
    string? CheckInLocalFormatted,
    string? CheckOutLocalFormatted,
    string ShiftName,
    TimeSpan? ShiftStart,
    TimeSpan? ShiftEnd,
    int GraceMinutes,
    string StatusBadge,
    bool ShiftHasEnded,
    int WorkedMinutesToday,
    int OvertimeMinutesToday,
    IReadOnlyList<BreakDto> BreaksToday,
    bool CanClockIn = true,
    string? ClockInBlockedReason = null
);

public record GetMyAttendanceSessionQuery(Guid EmployeeId);

public interface IGetMyAttendanceSessionHandler
{
    Task<MyAttendanceSessionDto> HandleAsync(GetMyAttendanceSessionQuery query, CancellationToken cancellationToken = default);
}

public class GetMyAttendanceSessionHandler : IGetMyAttendanceSessionHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly IClock _clock;

    public GetMyAttendanceSessionHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        IClock clock)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _clock = clock;
    }

    public async Task<MyAttendanceSessionDto> HandleAsync(GetMyAttendanceSessionQuery query, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var nowUtc = _clock.UtcNow;
        var nowLocal = _clock.ToLocal(nowUtc);

        var shift = await _calendarQuery.GetShiftForEmployeeAsync(query.EmployeeId, today, cancellationToken);
        var day = await _context.AttendanceDays
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmployeeId == query.EmployeeId && a.WorkDate == today, cancellationToken);

        List<BreakDto> breakDtos = new();
        bool isOnBreak = false;

        if (day != null)
        {
            var breaks = await _context.AttendanceBreaks
                .AsNoTracking()
                .Where(b => b.AttendanceDayId == day.AttendanceDayId)
                .OrderBy(b => b.BreakStartUtc)
                .ToListAsync(cancellationToken);

            foreach (var b in breaks)
            {
                var startLocal = _clock.ToLocal(b.BreakStartUtc).ToString("HH:mm");
                var endLocal = b.BreakEndUtc.HasValue ? _clock.ToLocal(b.BreakEndUtc.Value).ToString("HH:mm") : null;
                int duration = b.Minutes;
                if (!b.BreakEndUtc.HasValue)
                {
                    isOnBreak = true;
                    duration = (int)Math.Max(0, (nowUtc - b.BreakStartUtc).TotalMinutes);
                }

                breakDtos.Add(new BreakDto(
                    b.AttendanceBreakId,
                    b.BreakStartUtc,
                    b.BreakEndUtc,
                    startLocal,
                    endLocal,
                    duration
                ));
            }
        }

        bool hasClockedIn = day?.CheckInUtc != null;
        bool hasClockedOut = day?.CheckOutUtc != null;

        string? checkInFormatted = null;
        string? checkOutFormatted = null;

        if (day?.CheckInUtc != null)
        {
            var localIn = _clock.ToLocal(day.CheckInUtc.Value);
            checkInFormatted = $"Clocked in at {localIn:HH:mm} on {localIn:MMM dd} via Web";
        }

        if (day?.CheckOutUtc != null)
        {
            var localOut = _clock.ToLocal(day.CheckOutUtc.Value);
            checkOutFormatted = $"Clocked out at {localOut:HH:mm} on {localOut:MMM dd}";
        }

        string statusBadge = "Not Started";
        if (hasClockedIn && !hasClockedOut)
        {
            statusBadge = isOnBreak ? "On Break" : "Working";
        }
        else if (hasClockedOut)
        {
            statusBadge = "Completed";
        }

        bool shiftHasEnded = false;
        bool canClockIn = false;
        string? clockInBlockedReason = null;

        if (shift != null)
        {
            var shiftStart = TimeOnly.FromTimeSpan(shift.StartTime);
            var shiftEnd = TimeOnly.FromTimeSpan(shift.EndTime);
            var shiftStartLocal = today.ToDateTime(shiftStart);
            var shiftEndLocal = (shift.IsNightShift || shiftEnd < shiftStart)
                ? today.AddDays(1).ToDateTime(shiftEnd)
                : today.ToDateTime(shiftEnd);

            shiftHasEnded = nowLocal > shiftEndLocal;

            if (!hasClockedIn)
            {
                var earliestAllowed = shiftStartLocal.AddMinutes(-30);
                if (nowLocal < earliestAllowed)
                {
                    canClockIn = false;
                    clockInBlockedReason = $"Shift starts at {shiftStart:hh\\:mm\\ tt}. Clock-in opens at {earliestAllowed:hh\\:mm\\ tt}.";
                }
                else if (nowLocal > shiftEndLocal)
                {
                    canClockIn = false;
                    clockInBlockedReason = $"Shift ended at {shiftEnd:hh\\:mm\\ tt}. Clock-in is closed for today.";
                }
                else
                {
                    canClockIn = true;
                }
            }
        }
        else
        {
            canClockIn = false;
            clockInBlockedReason = "No scheduled shift found for today.";
        }

        int workedMins = day?.WorkedMinutes ?? 0;
        if (hasClockedIn && !hasClockedOut)
        {
            int totalElapsed = (int)Math.Max(0, (nowUtc - day!.CheckInUtc!.Value).TotalMinutes);
            int breakMins = breakDtos.Sum(b => b.Minutes);
            workedMins = Math.Max(0, totalElapsed - breakMins);
        }

        int overtimeMins = Math.Max(0, workedMins - 480); // standard 8 hours (480 mins)

        return new MyAttendanceSessionDto(
            HasClockedIn: hasClockedIn,
            IsOnBreak: isOnBreak,
            HasClockedOut: hasClockedOut,
            CheckInUtc: day?.CheckInUtc,
            CheckOutUtc: day?.CheckOutUtc,
            CheckInLocalFormatted: checkInFormatted,
            CheckOutLocalFormatted: checkOutFormatted,
            ShiftName: shift?.ShiftName ?? "Default Shift",
            ShiftStart: shift?.StartTime,
            ShiftEnd: shift?.EndTime,
            GraceMinutes: shift?.GraceMinutes > 0 ? shift.GraceMinutes : 15,
            StatusBadge: statusBadge,
            ShiftHasEnded: shiftHasEnded,
            WorkedMinutesToday: workedMins,
            OvertimeMinutesToday: overtimeMins,
            BreaksToday: breakDtos,
            CanClockIn: canClockIn,
            ClockInBlockedReason: clockInBlockedReason
        );
    }
}
