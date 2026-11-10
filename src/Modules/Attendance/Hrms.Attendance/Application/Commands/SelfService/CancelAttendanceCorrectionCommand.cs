using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record CancelAttendanceCorrectionCommand(Guid RequestId, Guid EmployeeId);

public interface ICancelAttendanceCorrectionHandler
{
    Task<Result> HandleAsync(CancelAttendanceCorrectionCommand command, CancellationToken cancellationToken = default);
}

public class CancelAttendanceCorrectionHandler : ICancelAttendanceCorrectionHandler
{
    private readonly IAttendanceDbContext _context;

    public CancelAttendanceCorrectionHandler(IAttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<Result> HandleAsync(CancelAttendanceCorrectionCommand command, CancellationToken cancellationToken = default)
    {
        var request = await _context.AttendanceRequests
            .FirstOrDefaultAsync(r => r.AttendanceRequestId == command.RequestId && r.EmployeeId == command.EmployeeId, cancellationToken);

        if (request == null)
        {
            return Result.Failure(Error.NotFound("Attendance regularization request not found."));
        }

        if (request.Status != AttendanceRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("Only pending requests can be cancelled. This request has already been reviewed."));
        }

        _context.AttendanceRequests.Remove(request);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
