using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Calendar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Calendar.Controllers;

[Area("Calendar")]
[Authorize(Policy = Permissions.Holidays.View)]
public class HolidaysController : Controller
{
    private readonly IHolidayService _holidayService;
    private readonly IWorkingDayService _workingDayService;

    public HolidaysController(
        IHolidayService holidayService,
        IWorkingDayService workingDayService)
    {
        _holidayService = holidayService;
        _workingDayService = workingDayService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, HolidayType? type)
    {
        var currentYear = DateTime.UtcNow.Year;
        var selectedYear = year ?? currentYear;
        var selectedMonth = month ?? DateTime.UtcNow.Month;

        var holidays = await _holidayService.GetHolidaysAsync(selectedYear, type);
        var workingDays = await _workingDayService.GetWorkingDaysAsync();

        var allHolidays = await _holidayService.GetHolidaysAsync();
        var availableYears = allHolidays.Select(h => h.HolidayDate.Year)
            .Append(currentYear - 1)
            .Append(currentYear)
            .Append(currentYear + 1)
            .Distinct()
            .OrderByDescending(y => y)
            .ToList();

        var model = new HolidayListViewModel
        {
            Holidays = holidays,
            WorkingDays = workingDays,
            SelectedYear = selectedYear,
            SelectedType = type,
            AvailableYears = availableYears,
            ViewMonth = selectedMonth,
            ViewYear = selectedYear
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    public IActionResult Create()
    {
        return View(new HolidayFormViewModel());
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HolidayFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateHolidayCommand(model.Name, model.HolidayDate, model.Type);
        var result = await _holidayService.CreateHolidayAsync(command);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Holiday '{model.Name}' created successfully for {model.HolidayDate:yyyy-MM-dd}.";
        return RedirectToAction(nameof(Index), new { year = model.HolidayDate.Year, month = model.HolidayDate.Month });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMultiDay(MultiDayHolidayFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Invalid holiday inputs. Please check the dates.";
            return RedirectToAction(nameof(Index));
        }

        var command = new CreateMultiDayHolidayCommand(model.Name, model.StartDate, model.EndDate, model.Type);
        var result = await _holidayService.CreateMultiDayHolidayAsync(command);

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(Index), new { year = model.StartDate.Year, month = model.StartDate.Month });
        }

        TempData["SuccessMessage"] = $"Successfully added {result.Value.Count} holiday days for '{model.Name}' ({model.StartDate:yyyy-MM-dd} to {model.EndDate:yyyy-MM-dd}).";
        return RedirectToAction(nameof(Index), new { year = model.StartDate.Year, month = model.StartDate.Month });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var holiday = await _holidayService.GetByIdAsync(id);
        if (holiday == null)
        {
            TempData["ErrorMessage"] = "Holiday not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new HolidayFormViewModel
        {
            HolidayId = holiday.HolidayId,
            Name = holiday.Name,
            HolidayDate = holiday.HolidayDate,
            Type = holiday.Type
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(HolidayFormViewModel model)
    {
        if (!ModelState.IsValid || !model.HolidayId.HasValue)
        {
            return View(model);
        }

        var command = new UpdateHolidayCommand(model.HolidayId.Value, model.Name, model.HolidayDate, model.Type);
        var result = await _holidayService.UpdateHolidayAsync(command);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Holiday '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index), new { year = model.HolidayDate.Year, month = model.HolidayDate.Month });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, int? returnYear = null, int? returnMonth = null)
    {
        var result = await _holidayService.DeleteHolidayAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Holiday deleted successfully.";
        }

        return RedirectToAction(nameof(Index), new { year = returnYear, month = returnMonth });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Holidays.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMultiple(List<Guid> holidayIds, int? returnYear = null, int? returnMonth = null)
    {
        if (holidayIds == null || !holidayIds.Any())
        {
            TempData["ErrorMessage"] = "Please select at least one holiday to delete.";
            return RedirectToAction(nameof(Index), new { year = returnYear, month = returnMonth });
        }

        var result = await _holidayService.DeleteHolidaysMultipleAsync(holidayIds);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Successfully deleted {result.Value} holiday(s).";
        }

        return RedirectToAction(nameof(Index), new { year = returnYear, month = returnMonth });
    }
}
