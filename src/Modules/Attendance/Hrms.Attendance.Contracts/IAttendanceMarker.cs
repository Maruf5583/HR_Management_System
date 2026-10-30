namespace Hrms.Attendance.Contracts;

public interface IAttendanceMarker
{
    Task MarkOnLeaveAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        CancellationToken cancellationToken = default);

    Task RecalculateDaysAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default);
}
