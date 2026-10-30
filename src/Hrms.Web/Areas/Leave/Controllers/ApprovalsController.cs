using Hrms.Leave.Application.Commands.Approvals;
using Hrms.Leave.Application.Queries.Approvals;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Leave.Controllers;

[Area("Leave")]
[Authorize]
public class ApprovalsController : Controller
{
    private readonly IGetApprovalsHandler _approvalsHandler;
    private readonly IApproveLeaveRequestHandler _approveHandler;
    private readonly IRejectLeaveRequestHandler _rejectHandler;
    private readonly IGetLeaveTypesHandler _typesHandler;

    public ApprovalsController(
        IGetApprovalsHandler approvalsHandler,
        IApproveLeaveRequestHandler approveHandler,
        IRejectLeaveRequestHandler rejectHandler,
        IGetLeaveTypesHandler typesHandler)
    {
        _approvalsHandler = approvalsHandler;
        _approveHandler = approveHandler;
        _rejectHandler = rejectHandler;
        _typesHandler = typesHandler;
    }

    [Authorize(Policy = Permissions.Leave.Approve)]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        Guid? leaveTypeId,
        DateOnly? fromDate,
        DateOnly? toDate,
        int page = 1)
    {
        const int pageSize = 15;
        var query = new GetPendingApprovalsQuery(
            SearchTerm: search,
            LeaveTypeId: leaveTypeId,
            FromDate: fromDate,
            ToDate: toDate,
            PageNumber: page,
            PageSize: pageSize
        );

        var result = await _approvalsHandler.GetPendingListAsync(query);

        var types = await _typesHandler.HandleAsync(new GetLeaveTypesQuery());
        ViewBag.LeaveTypes = new SelectList(types, "LeaveTypeId", "Name", leaveTypeId);
        ViewBag.Search = search;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

        return View(result);
    }

    [Authorize(Policy = Permissions.Leave.Approve)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var details = await _approvalsHandler.GetDetailsAsync(id);
        if (details == null)
        {
            TempData["ErrorMessage"] = "Leave request not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(details);
    }

    [Authorize(Policy = Permissions.Leave.Approve)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var result = await _approveHandler.HandleAsync(new ApproveLeaveRequestCommand(id));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Leave request approved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Leave.Approve)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reviewNote)
    {
        var result = await _rejectHandler.HandleAsync(new RejectLeaveRequestCommand(id, reviewNote));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["SuccessMessage"] = "Leave request rejected.";
        return RedirectToAction(nameof(Index));
    }
}
