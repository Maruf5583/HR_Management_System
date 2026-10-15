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
public class DesignationsController : Controller
{
    private readonly IDesignationService _designationService;
    private readonly IDepartmentService _departmentService;

    public DesignationsController(
        IDesignationService designationService,
        IDepartmentService departmentService)
    {
        _designationService = designationService;
        _departmentService = departmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? departmentId)
    {
        var designations = await _designationService.GetDesignationsAsync(departmentId);
        var departments = await _departmentService.GetDepartmentsAsync();

        ViewBag.Departments = departments.Where(d => d.Status == EntityStatus.Active)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == departmentId))
            .ToList();
        ViewBag.SelectedDepartmentId = departmentId;

        return View(designations);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? departmentId)
    {
        var model = new DesignationFormViewModel
        {
            DepartmentId = departmentId ?? Guid.Empty,
            DepartmentOptions = await GetDepartmentSelectListAsync(departmentId)
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DesignationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmentOptions = await GetDepartmentSelectListAsync(model.DepartmentId);
            return View(model);
        }

        var result = await _designationService.CreateDesignationAsync(new CreateDesignationModel(model.Title, model.DepartmentId));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.DepartmentOptions = await GetDepartmentSelectListAsync(model.DepartmentId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Designation '{model.Title}' created successfully.";
        return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var designation = await _designationService.GetDesignationByIdAsync(id);
        if (designation == null)
        {
            TempData["ErrorMessage"] = "Designation not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new DesignationFormViewModel
        {
            Id = designation.Id,
            Title = designation.Title,
            DepartmentId = designation.DepartmentId,
            DepartmentOptions = await GetDepartmentSelectListAsync(designation.DepartmentId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DesignationFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.DepartmentOptions = await GetDepartmentSelectListAsync(model.DepartmentId);
            return View(model);
        }

        var result = await _designationService.UpdateDesignationAsync(new UpdateDesignationModel(id, model.Title, model.DepartmentId));
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.DepartmentOptions = await GetDepartmentSelectListAsync(model.DepartmentId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Designation '{model.Title}' updated successfully.";
        return RedirectToAction(nameof(Index), new { departmentId = model.DepartmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id, EntityStatus status)
    {
        var result = await _designationService.SetDesignationStatusAsync(id, status);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Designation status changed to {status}.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetDepartmentSelectListAsync(Guid? selectedId)
    {
        var departments = await _departmentService.GetDepartmentsAsync();
        return departments.Where(d => d.Status == EntityStatus.Active)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == selectedId))
            .ToList();
    }
}
