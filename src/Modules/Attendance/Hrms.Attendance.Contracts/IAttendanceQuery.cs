namespace Hrms.Attendance.Contracts;

public record AttendanceMonthSummaryDto(
    Guid EmployeeId,
    int PresentDays,
    int AbsentDays,
    int HalfDays,
    int LateDays,
    int OnLeaveDays,
    int HolidayDays,
    int WeekendDays
);

public interface IAttendanceQuery
{
    Task<AttendanceMonthSummaryDto> GetMonthlySummaryAsync(
        Guid employeeId,
        DateOnly monthStart,
        CancellationToken cancellationToken = default);
}
