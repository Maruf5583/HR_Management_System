using FluentValidation;
using Hrms.Settings.Application.Models;
using Hrms.Settings.Application.Services;
using Hrms.Settings.Contracts;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Settings.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Settings.Controllers;

[Area("Settings")]
[Authorize(Policy = Permissions.Settings.Manage)]
public class SettingsController : Controller
{
    private readonly ISettingsService _settingsService;
    private readonly IValidator<UpdateSettingsCommand> _validator;
    private readonly IWebHostEnvironment _environment;

    private static readonly string[] AllowedLogoExtensions = { ".png", ".jpg", ".jpeg", ".svg", ".webp" };
    private static readonly string[] AllowedFaviconExtensions = { ".ico", ".png", ".svg" };

    public SettingsController(
        ISettingsService settingsService,
        IValidator<UpdateSettingsCommand> validator,
        IWebHostEnvironment environment)
    {
        _settingsService = settingsService;
        _validator = validator;
        _environment = environment;
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

            CompanyLogo = dict.TryGetValue(SettingKeys.CompanyLogo, out var cl) ? cl.ValueText : null,
            CompanyFavicon = dict.TryGetValue(SettingKeys.CompanyFavicon, out var cf) ? cf.ValueText : null,

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
        // 1. Process Logo File
        string? logoPath = model.CompanyLogo;
        if (model.RemoveLogo)
        {
            logoPath = string.Empty;
        }
        else if (model.LogoFile != null && model.LogoFile.Length > 0)
        {
            var ext = Path.GetExtension(model.LogoFile.FileName).ToLowerInvariant();
            if (!AllowedLogoExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo must be a valid image file (.png, .jpg, .jpeg, .svg, .webp).");
                return View("Index", model);
            }

            if (model.LogoFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.LogoFile), "Logo file size cannot exceed 2 MB.");
                return View("Index", model);
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "branding");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"logo_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.LogoFile.CopyToAsync(stream, cancellationToken);
            }

            logoPath = $"/uploads/branding/{fileName}";
        }

        // 2. Process Favicon File
        string? faviconPath = model.CompanyFavicon;
        if (model.RemoveFavicon)
        {
            faviconPath = string.Empty;
        }
        else if (model.FaviconFile != null && model.FaviconFile.Length > 0)
        {
            var ext = Path.GetExtension(model.FaviconFile.FileName).ToLowerInvariant();
            if (!AllowedFaviconExtensions.Contains(ext))
            {
                ModelState.AddModelError(nameof(model.FaviconFile), "Favicon must be a valid icon file (.ico, .png, .svg).");
                return View("Index", model);
            }

            if (model.FaviconFile.Length > 512 * 1024)
            {
                ModelState.AddModelError(nameof(model.FaviconFile), "Favicon file size cannot exceed 512 KB.");
                return View("Index", model);
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "branding");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"favicon_{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.FaviconFile.CopyToAsync(stream, cancellationToken);
            }

            faviconPath = $"/uploads/branding/{fileName}";
        }

        var command = new UpdateSettingsCommand(
            model.CompanyName ?? string.Empty,
            model.CompanyTimezone ?? string.Empty,
            model.AttendanceHalfDayHours ?? string.Empty,
            model.AttendanceHolidayPriority ?? string.Empty,
            model.LeaveYearStart ?? string.Empty,
            model.PayrollPerDayDivisor ?? string.Empty,
            model.EmployeeCodeFormat ?? string.Empty,
            logoPath,
            faviconPath
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

        TempData["SuccessMessage"] = "System settings and branding updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
