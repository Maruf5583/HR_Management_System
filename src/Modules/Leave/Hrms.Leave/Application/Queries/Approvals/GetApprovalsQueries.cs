using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Queries.Approvals;

public record PendingApprovalItemDto(
    Guid LeaveRecordId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    decimal TotalDays,
    string? Reason,
    DateTime AppliedAtUtc
);

public record ApprovalDetailsDto(
    Guid LeaveRecordId,
    Guid EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    Guid LeaveTypeId,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsHalfDay,
    decimal TotalDays,
    string? Reason,
    DateTime AppliedAtUtc,
    LeaveRecordStatus Status,
    // Employee's current balance for this leave type and year
    int Year,
    decimal Entitled,
    decimal Used,
    decimal Remaining
);

public record GetPendingApprovalsQuery(
    string? SearchTerm = null,
    Guid? LeaveTypeId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    int PageNumber = 1,
    int PageSize = 10
);

public interface IGetApprovalsHandler
{
    Task<PagedResult<PendingApprovalItemDto>> GetPendingListAsync(GetPendingApprovalsQuery query, CancellationToken cancellationToken = default);
    Task<ApprovalDetailsDto?> GetDetailsAsync(Guid leaveRecordId, CancellationToken cancellationToken = default);
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}

public class GetApprovalsHandler : IGetApprovalsHandler
{
    private readonly ILeaveDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ILeaveYearHelper _yearHelper;

    public GetApprovalsHandler(
        ILeaveDbContext context,
        IEmployeeLookup employeeLookup,
        ILeaveYearHelper yearHelper)
    {
        _context = context;
        _employeeLookup = employeeLookup;
        _yearHelper = yearHelper;
    }

    public async Task<PagedResult<PendingApprovalItemDto>> GetPendingListAsync(GetPendingApprovalsQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.LeaveRecords
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == LeaveRecordStatus.Pending);

        if (query.LeaveTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.LeaveTypeId == query.LeaveTypeId.Value);
        }

        if (query.FromDate.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.StartDate >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.EndDate <= query.ToDate.Value);
        }

        var allEmployees = await _employeeLookup.GetActiveEmployeesDropdownAsync(cancellationToken);
        var empMap = allEmployees.ToDictionary(e => e.EmployeeId, e => (e.EmployeeCode, e.FullName));

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim();
            var matchedEmpIds = allEmployees
                .Where(e => e.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                            e.EmployeeCode.Contains(search, StringComparison.OrdinalIgnoreCase))
                .Select(e => e.EmployeeId)
                .ToList();

            dbQuery = dbQuery.Where(lr => matchedEmpIds.Contains(lr.EmployeeId));
        }

        var totalCount = await dbQuery.CountAsync(cancellationToken);

        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 10 : query.PageSize;

        // Oldest first per prompt rule
        var items = await dbQuery
            .OrderBy(lr => lr.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => new
            {
                lr.LeaveRecordId,
                lr.EmployeeId,
                lr.LeaveTypeId,
                LeaveTypeName = lr.LeaveType != null ? lr.LeaveType.Name : "Leave",
                lr.StartDate,
                lr.EndDate,
                lr.IsHalfDay,
                lr.TotalDays,
                lr.Reason,
                AppliedAtUtc = lr.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var dtoList = items.Select(i =>
        {
            empMap.TryGetValue(i.EmployeeId, out var emp);
            return new PendingApprovalItemDto(
                i.LeaveRecordId,
                i.EmployeeId,
                emp.EmployeeCode ?? "EMP-N/A",
                emp.FullName ?? "Unknown Employee",
                i.LeaveTypeId,
                i.LeaveTypeName,
                i.StartDate,
                i.EndDate,
                i.IsHalfDay,
                i.TotalDays,
                i.Reason,
                i.AppliedAtUtc
            );
        }).ToList();

        return new PagedResult<PendingApprovalItemDto>(dtoList, totalCount, pageNumber, pageSize);
    }

    public async Task<ApprovalDetailsDto?> GetDetailsAsync(Guid leaveRecordId, CancellationToken cancellationToken = default)
    {
        var record = await _context.LeaveRecords
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.LeaveRecordId == leaveRecordId, cancellationToken);

        if (record == null)
        {
            return null;
        }

        var emp = await _employeeLookup.GetBasicInfoAsync(record.EmployeeId, cancellationToken);
        var leaveYear = await _yearHelper.GetLeaveYearAsync(record.StartDate, cancellationToken);

        // Fetch balance for this employee, leave type and year
        var balance = await _context.LeaveBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.EmployeeId == record.EmployeeId &&
                                      b.LeaveTypeId == record.LeaveTypeId &&
                                      b.Year == leaveYear, cancellationToken);

        decimal entitled = balance?.Entitled ?? record.LeaveType?.DaysPerYear ?? 0m;
        decimal used = balance?.Used ?? 0m;
        decimal remaining = balance?.Remaining ?? (entitled - used);

        return new ApprovalDetailsDto(
            record.LeaveRecordId,
            record.EmployeeId,
            emp?.EmployeeCode ?? "EMP-N/A",
            emp?.FullName ?? "Unknown Employee",
            record.LeaveTypeId,
            record.LeaveType != null ? record.LeaveType.Name : "Leave",
            record.StartDate,
            record.EndDate,
            record.IsHalfDay,
            record.TotalDays,
            record.Reason,
            record.CreatedAtUtc,
            record.Status,
            leaveYear,
            entitled,
            used,
            remaining
        );
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRecords
            .AsNoTracking()
            .CountAsync(lr => lr.Status == LeaveRecordStatus.Pending, cancellationToken);
    }
}
