using Hrms.Leave.Application.Commands.LeaveRequests;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Leave.Application.Queries.MyLeave;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Leave.Controllers;

[Area("Leave")]
[Authorize]
public class MyLeaveController : Controller
{
    private readonly IGetMyLeaveSummaryHandler _summaryHandler;
    private readonly IApplyLeaveHandler _applyHandler;
    private readonly ICancelPendingLeaveHandler _cancelHandler;
    private readonly IGetLeaveTypesHandler _typesHandler;

    public MyLeaveController(
        IGetMyLeaveSummaryHandler summaryHandler,
        IApplyLeaveHandler applyHandler,
        ICancelPendingLeaveHandler cancelHandler,
        IGetLeaveTypesHandler typesHandler)
    {
        _summaryHandler = summaryHandler;
        _applyHandler = applyHandler;
        _cancelHandler = cancelHandler;
        _typesHandler = typesHandler;
    }

    [Authorize(Policy = Permissions.Leave.ViewOwn)]
    [HttpGet]
    public async Task<IActionResult> Index(string activeTab = "myleaves")
    {
        var summary = await _summaryHandler.HandleAsync();

        var activeTypes = await _typesHandler.HandleAsync(new GetLeaveTypesQuery(ActiveOnly: true));
        ViewBag.ActiveLeaveTypes = new SelectList(activeTypes, "LeaveTypeId", "Name");
        ViewBag.ActiveTab = activeTab;

        var applyModel = new ApplyLeaveViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        };

        var pageModel = new MyLeavePageViewModel
        {
            Summary = summary,
            Apply = applyModel
        };

        return View(pageModel);
    }

    [Authorize(Policy = Permissions.Leave.Apply)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(ApplyLeaveViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please fill in all required fields accurately.";
            return RedirectToAction(nameof(Index), new { activeTab = "request" });
        }

        var command = new ApplyLeaveCommand(
            model.LeaveTypeId,
            model.StartDate,
            model.EndDate,
            model.IsHalfDay,
            model.Reason
        );

        var result = await _applyHandler.HandleAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(Index), new { activeTab = "request" });
        }

        TempData["SuccessMessage"] = "Your leave request was submitted successfully and is currently pending approval.";
        return RedirectToAction(nameof(Index), new { activeTab = "myleaves" });
    }

    [Authorize(Policy = Permissions.Leave.Apply)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _cancelHandler.HandleAsync(new CancelPendingLeaveCommand(id));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Your pending leave request has been cancelled.";
        }

        return RedirectToAction(nameof(Index));
    }
}

public class MyLeavePageViewModel
{
    public MyLeaveSummaryDto Summary { get; set; } = default!;
    public ApplyLeaveViewModel Apply { get; set; } = new();
}

public class ApplyLeaveViewModel
{
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? Reason { get; set; }
}
