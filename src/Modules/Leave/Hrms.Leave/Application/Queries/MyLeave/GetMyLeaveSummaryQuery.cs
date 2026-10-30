using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Application.Services;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Application.Queries.MyLeave;

public record MyLeaveBalanceCardDto(
    Guid LeaveTypeId,
    string LeaveTypeName,
    string LeaveTypeCode,
    bool IsPaid,
    decimal Entitled,
    decimal Used,
    decimal Remaining
);

public record MyLeaveHistoryItemDto(
    Guid LeaveRecordId,
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

public record MyLeaveSummaryDto(
    bool HasLinkedEmployee,
    Guid? EmployeeId,
    int CurrentLeaveYear,
    IReadOnlyList<MyLeaveBalanceCardDto> Balances,
    IReadOnlyList<MyLeaveHistoryItemDto> History
);

public interface IGetMyLeaveSummaryHandler
{
    Task<MyLeaveSummaryDto> HandleAsync(CancellationToken cancellationToken = default);
}

public class GetMyLeaveSummaryHandler : IGetMyLeaveSummaryHandler
{
    private readonly ILeaveDbContext _context;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public GetMyLeaveSummaryHandler(
        ILeaveDbContext context,
        ILeaveYearHelper yearHelper,
        ICurrentUser currentUser,
        IClock clock)
    {
        _context = context;
        _yearHelper = yearHelper;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<MyLeaveSummaryDto> HandleAsync(CancellationToken cancellationToken = default)
    {
        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue || employeeId.Value == Guid.Empty)
        {
            return new MyLeaveSummaryDto(
                HasLinkedEmployee: false,
                EmployeeId: null,
                CurrentLeaveYear: DateTime.UtcNow.Year,
                Balances: Array.Empty<MyLeaveBalanceCardDto>(),
                History: Array.Empty<MyLeaveHistoryItemDto>()
            );
        }

        var currentYear = await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal, cancellationToken);

        // Fetch balances for employee and current leave year
        var balances = await _context.LeaveBalances
            .AsNoTracking()
            .Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId.Value && b.Year == currentYear)
            .OrderBy(b => b.LeaveType!.Name)
            .Select(b => new MyLeaveBalanceCardDto(
                b.LeaveTypeId,
                b.LeaveType != null ? b.LeaveType.Name : "Leave",
                b.LeaveType != null ? b.LeaveType.Code : "",
                b.LeaveType != null && b.LeaveType.IsPaid,
                b.Entitled,
                b.Used,
                b.Remaining
            ))
            .ToListAsync(cancellationToken);

        // Fetch history of all requests for employee, sorted newest first
        var history = await _context.LeaveRecords
            .AsNoTracking()
            .Include(r => r.LeaveType)
            .Where(r => r.EmployeeId == employeeId.Value)
            .OrderByDescending(r => r.StartDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .Select(r => new MyLeaveHistoryItemDto(
                r.LeaveRecordId,
                r.LeaveType != null ? r.LeaveType.Name : "Leave",
                r.StartDate,
                r.EndDate,
                r.IsHalfDay,
                r.TotalDays,
                r.Status,
                r.Reason,
                r.ReviewNote,
                r.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return new MyLeaveSummaryDto(
            HasLinkedEmployee: true,
            EmployeeId: employeeId.Value,
            CurrentLeaveYear: currentYear,
            Balances: balances,
            History: history
        );
    }
}
