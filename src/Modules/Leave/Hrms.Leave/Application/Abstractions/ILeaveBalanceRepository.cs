using Hrms.Leave.Domain.Entities;

namespace Hrms.Leave.Application.Abstractions;

public interface ILeaveBalanceRepository
{
    Task<LeaveBalance?> GetByEmployeeTypeYearAsync(Guid employeeId, Guid leaveTypeId, int year, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveBalance>> GetByEmployeeAndYearAsync(Guid employeeId, int year, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveBalance balance, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<LeaveBalance> balances, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveBalance balance, CancellationToken cancellationToken = default);
}
