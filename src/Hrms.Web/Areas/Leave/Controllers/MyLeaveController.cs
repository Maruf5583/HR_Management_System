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

        var today = DateOnly.FromDateTime(DateTime.Today);
        var applyModel = new ApplyLeaveViewModel
        {
            StartDate = TempData["SubmittedStartDate"] != null && DateOnly.TryParse(TempData["SubmittedStartDate"]?.ToString(), out var sDate) ? sDate : today,
            EndDate = TempData["SubmittedEndDate"] != null && DateOnly.TryParse(TempData["SubmittedEndDate"]?.ToString(), out var eDate) ? eDate : today,
            LeaveTypeId = TempData["SubmittedLeaveTypeId"] != null && Guid.TryParse(TempData["SubmittedLeaveTypeId"]?.ToString(), out var ltId) ? ltId : Guid.Empty,
            Reason = TempData["SubmittedReason"]?.ToString(),
            IsHalfDay = TempData["SubmittedIsHalfDay"]?.ToString() == "true"
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
    public async Task<IActionResult> Apply([Bind(Prefix = "Apply")] ApplyLeaveViewModel model)
    {
        // Safe fallback in case form was posted without "Apply." prefix or with it
        if (model.StartDate == default)
        {
            if (DateOnly.TryParse(Request.Form["Apply.StartDate"], out var s1)) model.StartDate = s1;
            else if (DateOnly.TryParse(Request.Form["StartDate"], out var s2)) model.StartDate = s2;
        }

        if (model.EndDate == default)
        {
            if (DateOnly.TryParse(Request.Form["Apply.EndDate"], out var e1)) model.EndDate = e1;
            else if (DateOnly.TryParse(Request.Form["EndDate"], out var e2)) model.EndDate = e2;
        }

        if (model.LeaveTypeId == Guid.Empty)
        {
            if (Guid.TryParse(Request.Form["Apply.LeaveTypeId"], out var lt1)) model.LeaveTypeId = lt1;
            else if (Guid.TryParse(Request.Form["LeaveTypeId"], out var lt2)) model.LeaveTypeId = lt2;
        }

        if (string.IsNullOrWhiteSpace(model.Reason))
        {
            model.Reason = Request.Form["Apply.Reason"].FirstOrDefault() ?? Request.Form["Reason"].FirstOrDefault();
        }

        if (model.StartDate == default || model.EndDate == default || model.LeaveTypeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "Please fill in all required fields accurately.";
            TempData["SubmittedStartDate"] = model.StartDate != default ? model.StartDate.ToString("yyyy-MM-dd") : null;
            TempData["SubmittedEndDate"] = model.EndDate != default ? model.EndDate.ToString("yyyy-MM-dd") : null;
            TempData["SubmittedLeaveTypeId"] = model.LeaveTypeId != Guid.Empty ? model.LeaveTypeId.ToString() : null;
            TempData["SubmittedReason"] = model.Reason;
            TempData["SubmittedIsHalfDay"] = model.IsHalfDay ? "true" : "false";
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
            TempData["SubmittedStartDate"] = model.StartDate.ToString("yyyy-MM-dd");
            TempData["SubmittedEndDate"] = model.EndDate.ToString("yyyy-MM-dd");
            TempData["SubmittedLeaveTypeId"] = model.LeaveTypeId.ToString();
            TempData["SubmittedReason"] = model.Reason;
            TempData["SubmittedIsHalfDay"] = model.IsHalfDay ? "true" : "false";
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
