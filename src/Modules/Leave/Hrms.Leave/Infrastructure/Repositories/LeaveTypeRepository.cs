using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Entities;
using Hrms.Leave.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly ILeaveDbContext _context;

    public LeaveTypeRepository(ILeaveDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveType>> GetAllAsync(bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveTypes.AsQueryable();
        if (activeOnly)
        {
            query = query.Where(lt => lt.Status == LeaveTypeStatus.Active);
        }

        return await query.OrderBy(lt => lt.Name).ToListAsync(cancellationToken);
    }

    public async Task<LeaveType?> GetByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes.FirstOrDefaultAsync(lt => lt.LeaveTypeId == leaveTypeId, cancellationToken);
    }

    public async Task<LeaveType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await _context.LeaveTypes.FirstOrDefaultAsync(lt => lt.Code == normalizedCode, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var trimmedName = name.Trim();
        var query = _context.LeaveTypes.Where(lt => lt.Name == trimmedName);
        if (excludeId.HasValue)
        {
            query = query.Where(lt => lt.LeaveTypeId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var query = _context.LeaveTypes.Where(lt => lt.Code == normalizedCode);
        if (excludeId.HasValue)
        {
            query = query.Where(lt => lt.LeaveTypeId != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        await _context.LeaveTypes.AddAsync(leaveType, cancellationToken);
    }

    public Task UpdateAsync(LeaveType leaveType, CancellationToken cancellationToken = default)
    {
        _context.LeaveTypes.Update(leaveType);
        return Task.CompletedTask;
    }
}
