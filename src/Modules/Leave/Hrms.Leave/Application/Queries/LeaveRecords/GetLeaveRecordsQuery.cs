using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Queries.LeaveRecords;

public record LeaveRecordItemDto(
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
    LeaveRecordStatus Status,
    string? Reason,
    string? ReviewNote,
    DateTime CreatedAtUtc
);

public record LeaveRecordDetailsDto(
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
    LeaveRecordStatus Status,
    string? Reason,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewNote,
    DateTime CreatedAtUtc
);

public record GetLeaveRecordsQuery(
    Guid? EmployeeId = null,
    Guid? LeaveTypeId = null,
    LeaveRecordStatus? Status = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? SearchTerm = null,
    int PageNumber = 1,
    int PageSize = 10
);

public interface IGetLeaveRecordsHandler
{
    Task<PagedResult<LeaveRecordItemDto>> HandleAsync(GetLeaveRecordsQuery query, CancellationToken cancellationToken = default);
    Task<LeaveRecordDetailsDto?> GetDetailsAsync(Guid leaveRecordId, CancellationToken cancellationToken = default);
}

public class GetLeaveRecordsHandler : IGetLeaveRecordsHandler
{
    private readonly ILeaveDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;

    public GetLeaveRecordsHandler(
        ILeaveDbContext context,
        IEmployeeLookup employeeLookup)
    {
        _context = context;
        _employeeLookup = employeeLookup;
    }

    public async Task<PagedResult<LeaveRecordItemDto>> HandleAsync(GetLeaveRecordsQuery query, CancellationToken cancellationToken = default)
    {
        var dbQuery = _context.LeaveRecords
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .AsQueryable();

        if (query.EmployeeId.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.EmployeeId == query.EmployeeId.Value);
        }

        if (query.LeaveTypeId.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.LeaveTypeId == query.LeaveTypeId.Value);
        }

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(lr => lr.Status == query.Status.Value);
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

        var items = await dbQuery
            .OrderByDescending(lr => lr.StartDate)
            .ThenByDescending(lr => lr.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(lr => new
            {
                lr.LeaveRecordId,
                lr.EmployeeId,
                lr.LeaveTypeId,
                LeaveTypeName = lr.LeaveType != null ? lr.LeaveType.Name : "Unknown",
                lr.StartDate,
                lr.EndDate,
                lr.IsHalfDay,
                lr.TotalDays,
                lr.Status,
                lr.Reason,
                lr.ReviewNote,
                lr.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var dtoList = items.Select(i =>
        {
            empMap.TryGetValue(i.EmployeeId, out var emp);
            return new LeaveRecordItemDto(
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
                i.Status,
                i.Reason,
                i.ReviewNote,
                i.CreatedAtUtc
            );
        }).ToList();

        return new PagedResult<LeaveRecordItemDto>(dtoList, totalCount, pageNumber, pageSize);
    }

    public async Task<LeaveRecordDetailsDto?> GetDetailsAsync(Guid leaveRecordId, CancellationToken cancellationToken = default)
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

        return new LeaveRecordDetailsDto(
            record.LeaveRecordId,
            record.EmployeeId,
            emp?.EmployeeCode ?? "EMP-N/A",
            emp?.FullName ?? "Unknown Employee",
            record.LeaveTypeId,
            record.LeaveType != null ? record.LeaveType.Name : "Unknown",
            record.StartDate,
            record.EndDate,
            record.IsHalfDay,
            record.TotalDays,
            record.Status,
            record.Reason,
            record.ReviewedByUserId,
            record.ReviewedAtUtc,
            record.ReviewNote,
            record.CreatedAtUtc
        );
    }
}
