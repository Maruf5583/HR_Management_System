using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Infrastructure.Repositories;

public class LeaveBalanceRepository : ILeaveBalanceRepository
{
    private readonly ILeaveDbContext _context;

    public LeaveBalanceRepository(ILeaveDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveBalance?> GetByEmployeeTypeYearAsync(
        Guid employeeId,
        Guid leaveTypeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .FirstOrDefaultAsync(lb => lb.EmployeeId == employeeId &&
                                       lb.LeaveTypeId == leaveTypeId &&
                                       lb.Year == year, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveBalance>> GetByEmployeeAndYearAsync(
        Guid employeeId,
        int year,
        CancellationToken cancellationToken = default)
    {
        return await _context.LeaveBalances
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.EmployeeId == employeeId && lb.Year == year)
            .OrderBy(lb => lb.LeaveType!.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default)
    {
        await _context.LeaveBalances.AddAsync(balance, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<LeaveBalance> balances, CancellationToken cancellationToken = default)
    {
        await _context.LeaveBalances.AddRangeAsync(balances, cancellationToken);
    }

    public Task UpdateAsync(LeaveBalance balance, CancellationToken cancellationToken = default)
    {
        _context.LeaveBalances.Update(balance);
        return Task.CompletedTask;
    }
}
