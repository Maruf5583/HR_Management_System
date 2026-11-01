using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Contracts;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Queries.GetAttendanceRequests;

public record AttendanceRequestDto(
    Guid RequestId,
    Guid EmployeeId,
    string? EmployeeCode,
    string? EmployeeName,
    DateOnly WorkDate,
    DateTime RequestedCheckInUtc,
    DateTime? RequestedCheckOutUtc,
    string RequestedCheckInLocal,
    string? RequestedCheckOutLocal,
    string Reason,
    string Status,
    Guid? ReviewedByUserId,
    DateTime? ReviewedAtUtc,
    string? ReviewRemarks,
    DateTime CreatedAtUtc
);

public record GetEmployeeAttendanceRequestsQuery(Guid EmployeeId);
public record GetPendingAttendanceRequestsQuery();

public interface IGetAttendanceRequestsHandler
{
    Task<IReadOnlyList<AttendanceRequestDto>> GetEmployeeRequestsAsync(GetEmployeeAttendanceRequestsQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AttendanceRequestDto>> GetPendingRequestsAsync(GetPendingAttendanceRequestsQuery query, CancellationToken cancellationToken = default);
}

public class GetAttendanceRequestsHandler : IGetAttendanceRequestsHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly IClock _clock;

    public GetAttendanceRequestsHandler(
        IAttendanceDbContext context,
        IEmployeeLookup employeeLookup,
        IClock clock)
    {
        _context = context;
        _employeeLookup = employeeLookup;
        _clock = clock;
    }

    public async Task<IReadOnlyList<AttendanceRequestDto>> GetEmployeeRequestsAsync(
        GetEmployeeAttendanceRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var requests = await _context.AttendanceRequests
            .AsNoTracking()
            .Where(r => r.EmployeeId == query.EmployeeId)
            .OrderByDescending(r => r.WorkDate)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return requests.Select(r => new AttendanceRequestDto(
            r.AttendanceRequestId,
            r.EmployeeId,
            null,
            null,
            r.WorkDate,
            r.RequestedCheckInUtc,
            r.RequestedCheckOutUtc,
            _clock.ToLocal(r.RequestedCheckInUtc).ToString("hh:mm tt"),
            r.RequestedCheckOutUtc.HasValue ? _clock.ToLocal(r.RequestedCheckOutUtc.Value).ToString("hh:mm tt") : null,
            r.Reason,
            r.Status.ToString(),
            r.ReviewedByUserId,
            r.ReviewedAtUtc,
            r.ReviewRemarks,
            r.CreatedAtUtc
        )).ToList();
    }

    public async Task<IReadOnlyList<AttendanceRequestDto>> GetPendingRequestsAsync(
        GetPendingAttendanceRequestsQuery query,
        CancellationToken cancellationToken = default)
    {
        var requests = await _context.AttendanceRequests
            .AsNoTracking()
            .Where(r => r.Status == AttendanceRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var list = new List<AttendanceRequestDto>();
        foreach (var r in requests)
        {
            var emp = await _employeeLookup.GetBasicInfoAsync(r.EmployeeId, cancellationToken);
            list.Add(new AttendanceRequestDto(
                r.AttendanceRequestId,
                r.EmployeeId,
                emp?.EmployeeCode,
                emp?.FullName,
                r.WorkDate,
                r.RequestedCheckInUtc,
                r.RequestedCheckOutUtc,
                _clock.ToLocal(r.RequestedCheckInUtc).ToString("hh:mm tt"),
                r.RequestedCheckOutUtc.HasValue ? _clock.ToLocal(r.RequestedCheckOutUtc.Value).ToString("hh:mm tt") : null,
                r.Reason,
                r.Status.ToString(),
                r.ReviewedByUserId,
                r.ReviewedAtUtc,
                r.ReviewRemarks,
                r.CreatedAtUtc
            ));
        }

        return list;
    }
}
