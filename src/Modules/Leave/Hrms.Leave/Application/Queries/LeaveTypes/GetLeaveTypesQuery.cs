using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Queries.LeaveTypes;

public record LeaveTypeDto(
    Guid LeaveTypeId,
    string Name,
    string Code,
    bool IsPaid,
    decimal DaysPerYear,
    bool RequiresDocument,
    LeaveTypeStatus Status
);

public record GetLeaveTypesQuery(bool ActiveOnly = false);

public interface IGetLeaveTypesHandler
{
    Task<IReadOnlyList<LeaveTypeDto>> HandleAsync(GetLeaveTypesQuery query, CancellationToken cancellationToken = default);
    Task<LeaveTypeDto?> GetByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default);
}

public class GetLeaveTypesHandler : IGetLeaveTypesHandler
{
    private readonly ILeaveDbContext _context;

    public GetLeaveTypesHandler(ILeaveDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> HandleAsync(GetLeaveTypesQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.LeaveTypes.AsNoTracking();
        if (query.ActiveOnly)
        {
            dbQuery = dbQuery.Where(lt => lt.Status == LeaveTypeStatus.Active);
        }

        return await dbQuery
            .OrderBy(lt => lt.Name)
            .Select(lt => new LeaveTypeDto(
                lt.LeaveTypeId,
                lt.Name,
                lt.Code,
                lt.IsPaid,
                lt.DaysPerYear,
                lt.RequiresDocument,
                lt.Status
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<LeaveTypeDto?> GetByIdAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveTypes
            .AsNoTracking()
            .Where(lt => lt.LeaveTypeId == leaveTypeId)
            .Select(lt => new LeaveTypeDto(
                lt.LeaveTypeId,
                lt.Name,
                lt.Code,
                lt.IsPaid,
                lt.DaysPerYear,
                lt.RequiresDocument,
                lt.Status
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
