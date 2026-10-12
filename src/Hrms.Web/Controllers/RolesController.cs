using Hrms.Identity.Application.Models;
using Hrms.Identity.Application.Services;
using Hrms.Shared.Constants;
using Hrms.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Controllers;

[Authorize(Policy = Permissions.Roles.Manage)]
public class RolesController : Controller
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var roles = await _roleService.GetRolesAsync();
        return View(roles);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.AllPermissions = Permissions.All;
        return View(new CreateRoleViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllPermissions = Permissions.All;
            return View(model);
        }

        var result = await _roleService.CreateRoleAsync(new CreateRoleModel(
            model.Name,
            model.Description,
            model.SelectedPermissions
        ));

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            ViewBag.AllPermissions = Permissions.All;
            return View(model);
        }

        TempData["SuccessMessage"] = $"Role '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        ViewBag.AllPermissions = Permissions.All;

        var model = new EditRoleViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            SelectedPermissions = role.Permissions.ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditRoleViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllPermissions = Permissions.All;
            return View(model);
        }

        var result = await _roleService.UpdateRoleAsync(new UpdateRoleModel(
            model.Id,
            model.Description,
            model.SelectedPermissions
        ));

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            ViewBag.AllPermissions = Permissions.All;
            return View(model);
        }

        TempData["SuccessMessage"] = $"Role '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
        {
            return NotFound();
        }

        return View(role);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var result = await _roleService.DeleteRoleAsync(id);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Role deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
