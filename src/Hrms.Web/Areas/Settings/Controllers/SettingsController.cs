using FluentValidation;
using Hrms.Settings.Application.Models;
using Hrms.Settings.Application.Services;
using Hrms.Settings.Contracts;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Settings.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Settings.Controllers;

[Area("Settings")]
[Authorize(Policy = Permissions.Settings.Manage)]
public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IValidator<UpdateSettingsCommand> _validator;

    public SettingsController(
        ISettingsService settingsService,
        IValidator<UpdateSettingsCommand> validator)
    {
        _settingsService = settingsService;
        _validator = validator;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAllSettingsAsync(cancellationToken);
        var dict = settings.ToDictionary(s => s.SettingKey, s => s);

        var model = new SettingsViewModel
        {
            CompanyName = dict.TryGetValue(SettingKeys.CompanyName, out var cn) ? cn.ValueText : string.Empty,
            CompanyNameDescription = cn?.Description,

            CompanyTimezone = dict.TryGetValue(SettingKeys.CompanyTimezone, out var ct) ? ct.ValueText : string.Empty,
            CompanyTimezoneDescription = ct?.Description,

            AttendanceHalfDayHours = dict.TryGetValue(SettingKeys.AttendanceHalfDayHours, out var ah) ? ah.ValueText : string.Empty,
            AttendanceHalfDayHoursDescription = ah?.Description,

            AttendanceHolidayPriority = dict.TryGetValue(SettingKeys.AttendanceHolidayPriority, out var hp) ? hp.ValueText : "Holiday",
            AttendanceHolidayPriorityDescription = hp?.Description,

            LeaveYearStart = dict.TryGetValue(SettingKeys.LeaveYearStart, out var ly) ? ly.ValueText : string.Empty,
            LeaveYearStartDescription = ly?.Description,

            PayrollPerDayDivisor = dict.TryGetValue(SettingKeys.PayrollPerDayDivisor, out var pd) ? pd.ValueText : string.Empty,
            PayrollPerDayDivisorDescription = pd?.Description,

            EmployeeCodeFormat = dict.TryGetValue(SettingKeys.EmployeeCodeFormat, out var ef) ? ef.ValueText : string.Empty,
            EmployeeCodeFormatDescription = ef?.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SettingsViewModel model, CancellationToken cancellationToken)
    {
        var command = new UpdateSettingsCommand(
            model.CompanyName ?? string.Empty,
            model.CompanyTimezone ?? string.Empty,
            model.AttendanceHalfDayHours ?? string.Empty,
            model.AttendanceHolidayPriority ?? string.Empty,
            model.LeaveYearStart ?? string.Empty,
            model.PayrollPerDayDivisor ?? string.Empty,
            model.EmployeeCodeFormat ?? string.Empty
        );

        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }
            return View("Index", model);
        }

        var result = await _settingsService.UpdateSettingsAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View("Index", model);
        }

        TempData["SuccessMessage"] = "System settings updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
