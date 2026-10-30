using Hrms.Leave.Application.Commands.LeaveBalances;
using Hrms.Leave.Application.Queries.LeaveBalances;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Leave.Application.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Leave.Controllers;

[Area("Leave")]
[Authorize]
public class LeaveBalancesController : Controller
{
    private readonly IGetLeaveBalancesHandler _balancesHandler;
    private readonly IStartNewLeaveYearHandler _startYearHandler;
    private readonly IGetLeaveTypesHandler _typesHandler;
    private readonly ILeaveYearHelper _yearHelper;
    private readonly IClock _clock;

    public LeaveBalancesController(
        IGetLeaveBalancesHandler balancesHandler,
        IStartNewLeaveYearHandler startYearHandler,
        IGetLeaveTypesHandler typesHandler,
        ILeaveYearHelper yearHelper,
        IClock clock)
    {
        _balancesHandler = balancesHandler;
        _startYearHandler = startYearHandler;
        _typesHandler = typesHandler;
        _yearHelper = yearHelper;
        _clock = clock;
    }

    [Authorize(Policy = Permissions.Leave.ViewAll)]
    [HttpGet]
    public async Task<IActionResult> Index(
        int? year,
        Guid? leaveTypeId,
        string? search,
        int page = 1)
    {
        const int pageSize = 15;
        int currentYear = year ?? await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal);

        var query = new GetLeaveBalancesQuery(
            Year: currentYear,
            LeaveTypeId: leaveTypeId,
            SearchTerm: search,
            PageNumber: page,
            PageSize: pageSize
        );

        var result = await _balancesHandler.HandleAsync(query);

        var leaveTypes = await _typesHandler.HandleAsync(new GetLeaveTypesQuery());
        ViewBag.LeaveTypes = new SelectList(leaveTypes, "LeaveTypeId", "Name", leaveTypeId);
        ViewBag.SelectedYear = currentYear;
        ViewBag.Search = search;
        ViewBag.SelectedLeaveTypeId = leaveTypeId;

        return View(result);
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpGet]
    public async Task<IActionResult> StartYear()
    {
        int currentYear = await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal);
        ViewBag.DefaultYear = currentYear;
        return View();
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartYear(int? year)
    {
        int targetYear = year ?? await _yearHelper.GetLeaveYearAsync(_clock.TodayLocal);

        var result = await _startYearHandler.HandleAsync(new StartNewLeaveYearCommand(targetYear));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(StartYear));
        }

        TempData["SuccessMessage"] = $"Leave year {targetYear} initialized successfully. Created {result.Value} new employee balance row(s).";
        return RedirectToAction(nameof(Index), new { year = targetYear });
    }
}
