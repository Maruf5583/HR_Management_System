using Hrms.Leave.Domain.Entities;

namespace Hrms.Leave.Application.Abstractions;

public interface ILeaveRecordRepository
{
    Task<LeaveRecord?> GetByIdAsync(Guid leaveRecordId, CancellationToken cancellationToken = default);
    Task<LeaveRecord?> GetByIdWithLockAsync(Guid leaveRecordId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlappingRecordsAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRecordId = null,
        bool useLock = false,
        CancellationToken cancellationToken = default);
    Task AddAsync(LeaveRecord record, CancellationToken cancellationToken = default);
    Task UpdateAsync(LeaveRecord record, CancellationToken cancellationToken = default);
}
