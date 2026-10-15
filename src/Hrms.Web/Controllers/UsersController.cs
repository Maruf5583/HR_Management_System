using Hrms.Employees.Contracts;
using Hrms.Identity.Application.Models;
using Hrms.Identity.Application.Services;
using Hrms.Identity.Domain.Enums;
using Hrms.Shared.Constants;
using Hrms.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Controllers;

[Authorize(Policy = Permissions.Users.Manage)]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly IEmployeeLookup _employeeLookup;

    public UsersController(
        IUserService userService,
        IRoleService roleService,
        IEmployeeLookup employeeLookup)
    {
        _userService = userService;
        _roleService = roleService;
        _employeeLookup = employeeLookup;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userService.GetUsersAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadRolesViewDataAsync();
        await LoadEmployeesViewDataAsync();
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadRolesViewDataAsync();
            await LoadEmployeesViewDataAsync(selectedEmployeeId: model.EmployeeId);
            return View(model);
        }

        var result = await _userService.CreateUserAsync(new CreateUserModel(
            model.Email,
            model.Password,
            model.RoleName,
            model.EmployeeId
        ));

        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await LoadRolesViewDataAsync();
            await LoadEmployeesViewDataAsync(selectedEmployeeId: model.EmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] = $"User '{model.Email}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        await LoadRolesViewDataAsync();
        await LoadEmployeesViewDataAsync(currentUserId: user.Id, selectedEmployeeId: user.EmployeeId);
        var model = new EditUserViewModel
        {
            Id = user.Id,
            Email = user.Email,
            RoleName = user.Roles.FirstOrDefault() ?? string.Empty,
            Status = user.Status,
            EmployeeId = user.EmployeeId
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadRolesViewDataAsync();
            await LoadEmployeesViewDataAsync(currentUserId: model.Id, selectedEmployeeId: model.EmployeeId);
            return View(model);
        }

        var updateResult = await _userService.UpdateUserAsync(new UpdateUserModel(
            model.Id,
            model.Email,
            model.RoleName,
            model.EmployeeId
        ));

        if (updateResult.IsFailure)
        {
            ModelState.AddModelError(string.Empty, updateResult.Error.Message);
            await LoadRolesViewDataAsync();
            await LoadEmployeesViewDataAsync(currentUserId: model.Id, selectedEmployeeId: model.EmployeeId);
            return View(model);
        }

        TempData["SuccessMessage"] = "User updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(Guid id, UserStatus status)
    {
        var result = await _userService.SetUserStatusAsync(id, status);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"User status updated to {status}.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(new ResetPasswordViewModel
        {
            Id = user.Id,
            Email = user.Email
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userService.ResetPasswordAsync(model.Id, model.NewPassword);
        if (result.IsFailure)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Password for '{model.Email}' has been reset successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadRolesViewDataAsync()
    {
        var roles = await _roleService.GetRolesAsync();
        ViewBag.Roles = roles.Select(r => r.Name).ToList();
    }

    private async Task LoadEmployeesViewDataAsync(Guid? currentUserId = null, Guid? selectedEmployeeId = null)
    {
        var allEmployees = await _employeeLookup.GetActiveEmployeesDropdownAsync();
        var allUsers = await _userService.GetUsersAsync();
        var usedEmployeeIds = allUsers
            .Where(u => u.EmployeeId.HasValue && u.Id != currentUserId)
            .Select(u => u.EmployeeId!.Value)
            .ToHashSet();

        var available = allEmployees
            .Where(e => !usedEmployeeIds.Contains(e.EmployeeId) || e.EmployeeId == selectedEmployeeId)
            .Select(e => new SelectListItem($"{e.FullName} ({e.EmployeeCode})", e.EmployeeId.ToString(), e.EmployeeId == selectedEmployeeId))
            .ToList();

        ViewBag.Employees = available;
    }
}
