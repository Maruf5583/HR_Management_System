using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Domain.Entities;
using Hrms.Attendance.Domain.Enums;
using Hrms.Calendar.Contracts;
using Hrms.Identity.Contracts;
using Hrms.Notifications.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Commands.Approvals;

public record ReviewAttendanceCorrectionCommand(
    Guid RequestId,
    Guid ReviewedByUserId,
    bool Approve,
    string? Remarks
);

public interface IReviewAttendanceCorrectionHandler
{
    Task<Result> HandleAsync(ReviewAttendanceCorrectionCommand command, CancellationToken cancellationToken = default);
}

public class ReviewAttendanceCorrectionHandler : IReviewAttendanceCorrectionHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly ICalendarQuery _calendarQuery;
    private readonly IIdentityService _identityService;
    private readonly INotificationSender _notificationSender;
    private readonly IClock _clock;

    public ReviewAttendanceCorrectionHandler(
        IAttendanceDbContext context,
        ICalendarQuery calendarQuery,
        IIdentityService identityService,
        INotificationSender notificationSender,
        IClock clock)
    {
        _context = context;
        _calendarQuery = calendarQuery;
        _identityService = identityService;
        _notificationSender = notificationSender;
        _clock = clock;
    }

    public async Task<Result> HandleAsync(ReviewAttendanceCorrectionCommand command, CancellationToken cancellationToken = default)
    {
        var request = await _context.AttendanceRequests
            .FirstOrDefaultAsync(r => r.AttendanceRequestId == command.RequestId, cancellationToken);

        if (request == null)
        {
            return Result.Failure(Error.NotFound("Attendance request not found."));
        }

        if (request.Status != AttendanceRequestStatus.Pending)
        {
            return Result.Failure(Error.Conflict("This request has already been processed."));
        }

        var nowUtc = _clock.UtcNow;
        request.ReviewedByUserId = command.ReviewedByUserId;
        request.ReviewedAtUtc = nowUtc;
        request.ReviewRemarks = command.Remarks;

        var employeeUserId = await _identityService.GetUserIdByEmployeeIdAsync(request.EmployeeId, cancellationToken);
        if (employeeUserId.HasValue && employeeUserId.Value == command.ReviewedByUserId)
        {
            return Result.Failure(Error.Validation("You cannot approve or reject your own attendance regularization request. An independent HR manager or Administrator must review it."));
        }

        if (command.Approve)
        {
            request.Status = AttendanceRequestStatus.Approved;

            var shift = await _calendarQuery.GetShiftForEmployeeAsync(request.EmployeeId, request.WorkDate, cancellationToken);
            var checkInLocal = _clock.ToLocal(request.RequestedCheckInUtc);

            int graceMinutes = shift?.GraceMinutes > 0 ? shift.GraceMinutes : 15;
            int lateMinutes = 0;
            var status = AttendanceStatus.Present;

            if (shift != null)
            {
                var shiftStart = shift.StartTime;
                var graceCutoff = shiftStart.Add(TimeSpan.FromMinutes(graceMinutes));
                if (checkInLocal.TimeOfDay > graceCutoff)
                {
                    status = AttendanceStatus.Late;
                    lateMinutes = (int)Math.Max(0, (checkInLocal.TimeOfDay - graceCutoff).TotalMinutes);
                }
            }

            int workedMinutes = 0;
            if (request.RequestedCheckOutUtc.HasValue && request.RequestedCheckOutUtc.Value >= request.RequestedCheckInUtc)
            {
                int totalElapsed = (int)(request.RequestedCheckOutUtc.Value - request.RequestedCheckInUtc).TotalMinutes;
                int breakMins = shift?.BreakMinutes ?? 0;
                workedMinutes = Math.Max(0, totalElapsed - breakMins);

                if (workedMinutes < 240 && status == AttendanceStatus.Present)
                {
                    status = AttendanceStatus.HalfDay;
                }
            }

            var day = await _context.AttendanceDays
                .FirstOrDefaultAsync(a => a.EmployeeId == request.EmployeeId && a.WorkDate == request.WorkDate, cancellationToken);

            if (day == null)
            {
                day = new AttendanceDay
                {
                    AttendanceDayId = Guid.NewGuid(),
                    EmployeeId = request.EmployeeId,
                    WorkDate = request.WorkDate,
                    ShiftVersionId = shift?.ShiftVersionId,
                    CheckInUtc = request.RequestedCheckInUtc,
                    CheckOutUtc = request.RequestedCheckOutUtc,
                    LateMinutes = lateMinutes,
                    WorkedMinutes = workedMinutes,
                    Status = status,
                    Note = $"Regularized: {request.Reason}"
                };
                _context.AttendanceDays.Add(day);
            }
            else
            {
                day.ShiftVersionId = shift?.ShiftVersionId;
                day.CheckInUtc = request.RequestedCheckInUtc;
                day.CheckOutUtc = request.RequestedCheckOutUtc;
                day.LateMinutes = lateMinutes;
                day.WorkedMinutes = workedMinutes;
                day.Status = status;
                day.Note = $"Regularized: {request.Reason}";
            }

            if (employeeUserId.HasValue)
            {
                await _notificationSender.SendAsync(
                    employeeUserId.Value,
                    "Attendance Request Approved",
                    $"Your attendance correction request for {request.WorkDate:dd MMM yyyy} has been approved.",
                    "/Attendance/MyAttendance?tab=fix-attendance",
                    cancellationToken);
            }
        }
        else
        {
            request.Status = AttendanceRequestStatus.Rejected;

            if (employeeUserId.HasValue)
            {
                var reasonText = string.IsNullOrWhiteSpace(command.Remarks) ? "No remarks provided" : command.Remarks.Trim();
                await _notificationSender.SendAsync(
                    employeeUserId.Value,
                    "Attendance Request Rejected",
                    $"Your attendance correction request for {request.WorkDate:dd MMM yyyy} was rejected. Note: {reasonText}",
                    "/Attendance/MyAttendance?tab=fix-attendance",
                    cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
