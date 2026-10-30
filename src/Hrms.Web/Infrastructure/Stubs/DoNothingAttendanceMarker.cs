using Hrms.Attendance.Contracts;

namespace Hrms.Web.Infrastructure.Stubs;

/// <summary>
/// Temporary do-nothing stub for IAttendanceMarker until the Attendance module is built.
/// To be removed when Hrms.Attendance is implemented.
/// </summary>
public class DoNothingAttendanceMarker : IAttendanceMarker
{
    public Task MarkOnLeaveAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        CancellationToken cancellationToken = default)
    {
        // Do nothing stub
        return Task.CompletedTask;
    }

    public Task RecalculateDaysAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        // Do nothing stub
        return Task.CompletedTask;
    }
}
