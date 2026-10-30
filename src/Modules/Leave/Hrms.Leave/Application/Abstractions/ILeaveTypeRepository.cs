using Hrms.Leave.Domain.Entities;

namespace Hrms.Leave.Application.Abstractions;

public interface ILeaveTypeRepository
{
    Task<IReadOnlyList<LeaveType>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default);
    Task<LeaveType?> GetByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
    Task<LeaveType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);
    Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveType leaveType, CancellationToken cancellationToken = default);
}
