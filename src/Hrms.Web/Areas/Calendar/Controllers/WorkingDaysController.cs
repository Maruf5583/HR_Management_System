using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Calendar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Calendar.Controllers;

[Area("Calendar")]
[Authorize(Policy = Permissions.Holidays.Manage)]
public class WorkingDaysController : Controller
{
    private readonly IWorkingDayService _workingDayService;

    public WorkingDaysController(IWorkingDayService workingDayService)
    {
        _workingDayService = workingDayService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var days = await _workingDayService.GetWorkingDaysAsync();
        var model = new WorkingDaysFormViewModel
        {
            Days = days.Select(d => new WorkingDayItemViewModel
            {
                DayOfWeek = d.DayOfWeek,
                DayName = d.DayName,
                IsWorkingDay = d.IsWorkingDay
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(WorkingDaysFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new UpdateWorkingDaysCommand(
            model.Days.Select(d => new UpdateWorkingDayItem(d.DayOfWeek, d.IsWorkingDay)).ToList()
        );

        var result = await _workingDayService.UpdateWorkingDaysAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = "Company working days schedule updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
