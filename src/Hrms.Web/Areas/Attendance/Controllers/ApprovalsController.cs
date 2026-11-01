using Hrms.Attendance.Application.Commands.Approvals;
using Hrms.Attendance.Application.Queries.GetAttendanceRequests;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Attendance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Attendance.Controllers;

[Area("Attendance")]
[Authorize(Policy = Permissions.Attendance.Manage)]
public class ApprovalsController : Controller
{
    private readonly IGetAttendanceRequestsHandler _attendanceRequestsHandler;
    private readonly IReviewAttendanceCorrectionHandler _reviewHandler;
    private readonly ICurrentUser _currentUser;

    public ApprovalsController(
        IGetAttendanceRequestsHandler attendanceRequestsHandler,
        IReviewAttendanceCorrectionHandler reviewHandler,
        ICurrentUser currentUser)
    {
        _attendanceRequestsHandler = attendanceRequestsHandler;
        _reviewHandler = reviewHandler;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pending = await _attendanceRequestsHandler.GetPendingRequestsAsync(new GetPendingAttendanceRequestsQuery());
        var vm = new AttendanceApprovalsViewModel
        {
            PendingRequests = pending
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewCorrectionInputModel model)
    {
        if (!_currentUser.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "User session expired or unauthorized.";
            return RedirectToAction(nameof(Index));
        }

        var command = new ReviewAttendanceCorrectionCommand(
            model.RequestId,
            _currentUser.UserId.Value,
            model.Approve,
            model.Remarks
        );

        var result = await _reviewHandler.HandleAsync(command);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = model.Approve
                ? "Attendance request approved and attendance day updated. Employee has been notified."
                : "Attendance request rejected. Employee has been notified.";
        }

        return RedirectToAction(nameof(Index));
    }
}
