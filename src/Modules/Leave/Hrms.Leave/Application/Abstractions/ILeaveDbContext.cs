using Hrms.Leave.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Abstractions;

public interface ILeaveDbContext
{
    DbSet<LeaveType> LeaveTypes { get; }
    DbSet<LeaveBalance> LeaveBalances { get; }
    DbSet<LeaveRecord> LeaveRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
