using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Employees.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Policy = Permissions.Employees.Manage)]
public class GradesController : Controller
{
    private readonly IGradeService _gradeService;
    private readonly IDesignationService _designationService;

    public GradesController(
        IGradeService gradeService,
        IDesignationService designationService)
    {
        _gradeService = gradeService;
        _designationService = designationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? designationId)
    {
        var grades = await _gradeService.GetGradesAsync(designationId);
        var designations = await _designationService.GetDesignationsAsync();

        ViewBag.Designations = designations.Where(d => d.Status == EntityStatus.Active)
            .Select(d => new SelectListItem($"{d.Title} ({d.DepartmentName})", d.Id.ToString(), d.Id == designationId))
            .ToList();
        ViewBag.SelectedDesignationId = designationId;

        return View(grades);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? designationId)
    {
        var model = new GradeFormViewModel
        {
            DesignationId = designationId ?? Guid.Empty,
            DesignationOptions = await GetDesignationSelectListAsync(designationId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(GradeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DesignationOptions = await GetDesignationSelectListAsync(model.DesignationId);
            return View(model);
        }

        var result = await _gradeService.CreateGradeAsync(new CreateGradeModel(model.DesignationId, model.Level, model.Code, model.Salary));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.DesignationOptions = await GetDesignationSelectListAsync(model.DesignationId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Grade '{model.Code}' created successfully.";
        return RedirectToAction(nameof(Index), new { designationId = model.DesignationId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var grade = await _gradeService.GetGradeByIdAsync(id);
        if (grade == null)
        {
            TempData["ErrorMessage"] = "Grade not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new GradeFormViewModel
        {
            Id = grade.Id,
            DesignationId = grade.DesignationId,
            Level = grade.Level,
            Code = grade.Code,
            Salary = grade.Salary,
            DesignationOptions = await GetDesignationSelectListAsync(grade.DesignationId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, GradeFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DesignationOptions = await GetDesignationSelectListAsync(model.DesignationId);
            return View(model);
        }

        var result = await _gradeService.UpdateGradeAsync(new UpdateGradeModel(id, model.DesignationId, model.Level, model.Code, model.Salary));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.DesignationOptions = await GetDesignationSelectListAsync(model.DesignationId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Grade '{model.Code}' updated successfully.";
        return RedirectToAction(nameof(Index), new { designationId = model.DesignationId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, EntityStatus status)
    {
        var result = await _gradeService.SetGradeStatusAsync(id, status);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Grade status changed to {status}.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetDesignationSelectListAsync(Guid? selectedId)
    {
        var designations = await _designationService.GetDesignationsAsync();
        return designations.Where(d => d.Status == EntityStatus.Active)
            .Select(d => new SelectListItem($"{d.Title} ({d.DepartmentName})", d.Id.ToString(), d.Id == selectedId))
            .ToList();
    }
}
