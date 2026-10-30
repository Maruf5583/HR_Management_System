using Hrms.Leave.Application.Abstractions;
using Hrms.Leave.Contracts;
using Hrms.Leave.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Leave.Infrastructure.Services;

public class LeaveQueryService : ILeaveQuery
{
    private readonly ILeaveDbContext _context;

    public LeaveQueryService(ILeaveDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsOnLeaveAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRecords
            .AsNoTracking()
            .AnyAsync(lr => lr.EmployeeId == employeeId &&
                            lr.Status == LeaveRecordStatus.Approved &&
                            lr.StartDate <= date &&
                            lr.EndDate >= date, cancellationToken);
    }

    public async Task<IReadOnlyList<DateOnly>> GetApprovedLeaveDatesAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var records = await _context.LeaveRecords
            .AsNoTracking()
            .Where(lr => lr.EmployeeId == employeeId &&
                         lr.Status == LeaveRecordStatus.Approved &&
                         lr.StartDate <= endDate &&
                         lr.EndDate >= startDate)
            .ToListAsync(cancellationToken);

        var dates = new HashSet<DateOnly>();
        foreach (var r in records)
        {
            var cur = r.StartDate < startDate ? startDate : r.StartDate;
            var max = r.EndDate > endDate ? endDate : r.EndDate;
            while (cur <= max)
            {
                dates.Add(cur);
                cur = cur.AddDays(1);
            }
        }

        return dates.OrderBy(d => d).ToList();
    }

    public async Task<decimal> GetUnpaidLeaveDaysAsync(
        Guid employeeId,
        DateOnly monthStart,
        CancellationToken cancellationToken = default)
    {
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var records = await _context.LeaveRecords
            .AsNoTracking()
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.EmployeeId == employeeId &&
                         lr.Status == LeaveRecordStatus.Approved &&
                         lr.LeaveType != null &&
                         !lr.LeaveType.IsPaid &&
                         lr.StartDate <= monthEnd &&
                         lr.EndDate >= monthStart)
            .ToListAsync(cancellationToken);

        decimal totalDays = 0;
        foreach (var r in records)
        {
            if (r.StartDate >= monthStart && r.EndDate <= monthEnd)
            {
                totalDays += r.TotalDays;
            }
            else
            {
                // Prorate working days in month
                var cur = r.StartDate < monthStart ? monthStart : r.StartDate;
                var max = r.EndDate > monthEnd ? monthEnd : r.EndDate;
                int days = max.DayNumber - cur.DayNumber + 1;
                totalDays += days;
            }
        }

        return totalDays;
    }
}
