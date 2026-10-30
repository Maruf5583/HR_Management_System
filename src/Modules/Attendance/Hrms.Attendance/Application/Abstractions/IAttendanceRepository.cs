using Hrms.Attendance.Domain.Entities;

namespace Hrms.Attendance.Application.Abstractions;

public interface IAttendanceRepository
{
    Task<AttendanceDay?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceDay>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceDay>> GetByEmployeeAndDateRangeAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task AddAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<AttendanceDay> attendanceDays, CancellationToken cancellationToken = default);
    Task UpdateAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
