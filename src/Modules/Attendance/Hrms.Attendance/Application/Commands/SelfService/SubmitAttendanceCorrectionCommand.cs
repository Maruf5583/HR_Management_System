using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Hrms.Attendance.Domain.Enums;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.SelfService;

public record SubmitAttendanceCorrectionCommand(
    Guid EmployeeId,
    DateOnly WorkDate,
    TimeOnly CheckInTime,
    TimeOnly? CheckOutTime,
    string Reason
);

public interface ISubmitAttendanceCorrectionHandler
{
    Task<Result> HandleAsync(SubmitAttendanceCorrectionCommand command, CancellationToken cancellationToken = default);
}

public class SubmitAttendanceCorrectionHandler : ISubmitAttendanceCorrectionHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IClock _clock;

    public SubmitAttendanceCorrectionHandler(IAttendanceDbContext context, IClock clock)
    {
        _context = context;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(SubmitAttendanceCorrectionCommand command, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        if (command.WorkDate > today)
        {
            return Result.Failure(Error.Validation("Attendance correction requests cannot be submitted for future dates."));
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return Result.Failure(Error.Validation("Please provide a valid reason for attendance regularization."));
        }

        if (command.CheckOutTime.HasValue && command.CheckOutTime.Value < command.CheckInTime)
        {
            return Result.Failure(Error.Validation("Check-out time cannot be earlier than check-in time."));
        }

        var existingPending = await _context.AttendanceRequests
            .AnyAsync(r => r.EmployeeId == command.EmployeeId && r.WorkDate == command.WorkDate && r.Status == AttendanceRequestStatus.Pending, cancellationToken);

        if (existingPending)
        {
            return Result.Failure(Error.Conflict("You already have a pending correction request for this date."));
        }

        var checkInDateTimeLocal = command.WorkDate.ToDateTime(command.CheckInTime);
        var checkInUtc = _clock.ToUtc(checkInDateTimeLocal);

        DateTime? checkOutUtc = null;
        if (command.CheckOutTime.HasValue)
        {
            var checkOutDateTimeLocal = command.WorkDate.ToDateTime(command.CheckOutTime.Value);
            checkOutUtc = _clock.ToUtc(checkOutDateTimeLocal);
        }

        var request = new AttendanceRequest
        {
            AttendanceRequestId = Guid.NewGuid(),
            EmployeeId = command.EmployeeId,
            WorkDate = command.WorkDate,
            RequestedCheckInUtc = checkInUtc,
            RequestedCheckOutUtc = checkOutUtc,
            Reason = command.Reason.Trim(),
            Status = AttendanceRequestStatus.Pending
        };

        _context.AttendanceRequests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
