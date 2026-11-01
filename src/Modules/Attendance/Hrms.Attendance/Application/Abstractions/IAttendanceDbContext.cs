using Hrms.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Abstractions;

public interface IAttendanceDbContext
{
    DbSet<AttendanceDay> AttendanceDays { get; }
    DbSet<AttendanceBreak> AttendanceBreaks { get; }
    DbSet<AttendanceRequest> AttendanceRequests { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
