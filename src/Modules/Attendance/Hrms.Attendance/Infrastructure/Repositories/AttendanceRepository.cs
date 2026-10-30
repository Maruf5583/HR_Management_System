using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Infrastructure.Repositories;

public class AttendanceRepository : IAttendanceRepository
{
    private readonly IAttendanceDbContext _context;

    public AttendanceRepository(IAttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceDay?> GetByEmployeeAndDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceDays
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == date, cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDay>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceDays
            .Where(a => a.WorkDate == date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AttendanceDay>> GetByEmployeeAndDateRangeAsync(Guid employeeId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.AttendanceDays
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= startDate && a.WorkDate <= endDate)
            .OrderBy(a => a.WorkDate)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceDays.AddAsync(attendanceDay, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<AttendanceDay> attendanceDays, CancellationToken cancellationToken = default)
    {
        await _context.AttendanceDays.AddRangeAsync(attendanceDays, cancellationToken);
    }

    public Task UpdateAsync(AttendanceDay attendanceDay, CancellationToken cancellationToken = default)
    {
        _context.AttendanceDays.Update(attendanceDay);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
