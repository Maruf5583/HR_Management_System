using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Infrastructure.Repositories;

public class LeaveRecordRepository : ILeaveRecordRepository
{
    private readonly ILeaveDbContext _context;

    public LeaveRecordRepository(ILeaveDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRecord?> GetByIdAsync(Guid leaveRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRecords
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.LeaveRecordId == leaveRecordId, cancellationToken);
    }

    public async Task<LeaveRecord?> GetByIdWithLockAsync(Guid leaveRecordId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRecords
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.LeaveRecordId == leaveRecordId, cancellationToken);
    }

    public async Task<bool> HasOverlappingRecordsAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRecordId = null,
        bool useLock = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRecords.Where(lr =>
            lr.EmployeeId == employeeId &&
            (lr.Status == LeaveRecordStatus.Pending || lr.Status == LeaveRecordStatus.Approved) &&
            lr.StartDate <= endDate &&
            lr.EndDate >= startDate);

        if (excludeRecordId.HasValue)
        {
            query = query.Where(lr => lr.LeaveRecordId != excludeRecordId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRecords.AddAsync(record, cancellationToken);
    }

    public Task UpdateAsync(LeaveRecord record, CancellationToken cancellationToken = default)
    {
        _context.LeaveRecords.Update(record);
        return Task.CompletedTask;
    }
}
