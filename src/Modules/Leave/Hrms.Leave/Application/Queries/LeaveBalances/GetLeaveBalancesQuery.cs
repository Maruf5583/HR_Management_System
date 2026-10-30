using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Queries.LeaveBalances;

public record LeaveBalanceItemDto(
    Guid LeaveBalanceId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal Entitled,
    decimal Used,
    decimal Remaining
);

public record GetLeaveBalancesQuery(
    int? Year = null,
    Guid? LeaveTypeId = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10
);

public interface IGetLeaveBalancesHandler
{
    Task<PagedResult<LeaveBalanceItemDto>> HandleAsync(GetLeaveBalancesQuery query, CancellationToken cancellationToken = default);
}

public class GetLeaveBalancesHandler : IGetLeaveBalancesHandler
{
    private readonly ILeaveDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly IClock _clock;

    public GetLeaveBalancesHandler(
        ILeaveDbContext context,
        IEmployeeLookup employeeLookup,
        ILeaveYearHelper yearHelper,
        IClock clock)
    {
        _context = context;
        _employeeLookup = employeeLookup;
        _yearHelper = yearHelper;
        _clock = clock;
    }

    public async Task<PagedResult<LeaveBalanceItemDto>> HandleAsync(GetLeaveBalancesQuery query, CancellationToken cancellationToken = default)
    {
        int year = query.Year ?? await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal, cancellationToken);

        var dbQuery = _context.LeaveBalances
            .AsNoTracking()
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.Year == year);

        if (query.LeaveTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(lb => lb.LeaveTypeId == query.LeaveTypeId.Value);
        }

        var allEmployees = await _employeeLookup.GetActiveEmployeesDropdownAsync(cancellationToken);
        var empMap = allEmployees.ToDictionary(e => e.EmployeeId, e => (e.EmployeeCode, e.FullName));

        // If search term is provided, filter employee IDs
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            var matchedEmpIds = allEmployees
                .Where(e => e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            e.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EmployeeId)
                .ToList();

            dbQuery = dbQuery.Where(lb => matchedEmpIds.Contains(lb.EmployeeId));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        var items = await dbQuery
            .OrderBy(lb => lb.EmployeeId)
            .ThenBy(lb => lb.LeaveType!.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(lb => new
            {
                lb.LeaveBalanceId,
                lb.EmployeeId,
                lb.LeaveTypeId,
                LeaveTypeName = lb.LeaveType != null ? lb.LeaveType.Name : "Unknown",
                lb.Year,
                lb.Entitled,
                lb.Used,
                lb.Remaining
            })
            .ToListAsync(cancellationToken);

        var dtoList = items.Select(i =>
        {
            empMap.TryGetValue(i.EmployeeId, out var emp);
            return new LeaveBalanceItemDto(
                i.LeaveBalanceId,
                i.EmployeeId,
                emp.EmployeeCode ?? "EMP-N/A",
                emp.FullName ?? "Unknown Employee",
                i.LeaveTypeId,
                i.LeaveTypeName,
                i.Year,
                i.Entitled,
                i.Used,
                i.Remaining
            );
        }).ToList();

        return new PagedResult<LeaveBalanceItemDto>(dtoList, totalCount, pageNumber, pageSize);
    }
}
