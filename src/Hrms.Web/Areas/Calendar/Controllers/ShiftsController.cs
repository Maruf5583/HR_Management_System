using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Calendar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Calendar.Controllers;

[Area("Calendar")]
[Authorize(Policy = Permissions.Shifts.Manage)]
public class ShiftsController : Controller
{
    private readonly IShiftService _shiftService;

    public ShiftsController(IShiftService shiftService)
    {
        _shiftService = shiftService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ShiftStatus? status)
    {
        var allShifts = await _shiftService.GetShiftsAsync();
        var filteredShifts = status.HasValue ? allShifts.Where(s => s.Status == status.Value).ToList() : allShifts;

        var model = new ShiftListViewModel
        {
            Shifts = filteredShifts,
            StatusFilter = status,
            TotalCount = allShifts.Count,
            ActiveCount = allShifts.Count(s => s.Status == ShiftStatus.Active),
            InactiveCount = allShifts.Count(s => s.Status == ShiftStatus.Inactive)
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ShiftCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ShiftCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateShiftCommand(
            model.Name,
            model.StartTime,
            model.EndTime,
            model.GraceMinutes,
            model.BreakMinutes,
            model.EffectiveFrom
        );

        var result = await _shiftService.CreateShiftAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Shift '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var shift = await _shiftService.GetShiftDetailsAsync(id);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new ShiftEditViewModel
        {
            ShiftId = shift.ShiftId,
            Name = shift.Name,
            Status = shift.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ShiftEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _shiftService.UpdateShiftNameAsync(model.ShiftId, model.Name);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Shift '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.ShiftId });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var details = await _shiftService.GetShiftDetailsAsync(id);
        if (details == null)
        {
            TempData["ErrorMessage"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        var assignments = await _shiftService.GetAssignmentsAsync(shiftId: id);

        var model = new ShiftDetailsViewModel
        {
            Details = details,
            RecentAssignments = assignments.Take(15).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateVersion(Guid shiftId)
    {
        var shift = await _shiftService.GetShiftDetailsAsync(shiftId);
        if (shift == null)
        {
            TempData["ErrorMessage"] = "Shift not found.";
            return RedirectToAction(nameof(Index));
        }

        var current = shift.Versions.FirstOrDefault();
        var model = new ShiftVersionCreateViewModel
        {
            ShiftId = shift.ShiftId,
            ShiftName = shift.Name,
            StartTime = current?.StartTime ?? new TimeSpan(9, 0, 0),
            EndTime = current?.EndTime ?? new TimeSpan(18, 0, 0),
            GraceMinutes = current?.GraceMinutes ?? 15,
            BreakMinutes = current?.BreakMinutes ?? 60,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateVersion(ShiftVersionCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateShiftVersionCommand(
            model.ShiftId,
            model.StartTime,
            model.EndTime,
            model.GraceMinutes,
            model.BreakMinutes,
            model.EffectiveFrom
        );

        var result = await _shiftService.CreateShiftVersionAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "New shift timing version scheduled successfully.";
        return RedirectToAction(nameof(Details), new { id = model.ShiftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid shiftId, ShiftStatus newStatus, string? returnUrl = null)
    {
        var result = await _shiftService.SetShiftStatusAsync(shiftId, newStatus);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Shift status updated to '{newStatus}'.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _shiftService.DeleteShiftAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Shift deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
