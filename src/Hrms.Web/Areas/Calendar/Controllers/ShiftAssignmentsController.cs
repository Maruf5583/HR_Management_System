using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Application.Services;
using Hrms.Calendar.Domain.Enums;
using Hrms.Employees.Contracts;
using Hrms.Employees.Domain.Enums;
using Hrms.Persistence;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Calendar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Web.Areas.Calendar.Controllers;

[Area("Calendar")]
[Authorize(Policy = Permissions.Shifts.Manage)]
public class ShiftAssignmentsController : Controller
{
    private readonly IShiftService _shiftService;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly HrmsDbContext _dbContext;

    public ShiftAssignmentsController(
        IShiftService shiftService,
        IEmployeeLookup employeeLookup,
        HrmsDbContext dbContext)
    {
        _shiftService = shiftService;
        _employeeLookup = employeeLookup;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid? shiftId, Guid? employeeId)
    {
        var assignments = await _shiftService.GetAssignmentsAsync(shiftId, employeeId);
        var shifts = await _shiftService.GetShiftsAsync();
        var employees = await _employeeLookup.GetActiveEmployeesDropdownAsync();

        var model = new ShiftAssignmentListViewModel
        {
            Assignments = assignments,
            SelectedShiftId = shiftId,
            SelectedEmployeeId = employeeId,
            Shifts = shifts,
            Employees = employees
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Assign(Guid? employeeId, Guid? shiftId, CancellationToken cancellationToken = default)
    {
        var model = new AssignShiftViewModel
        {
            EmployeeId = employeeId ?? Guid.Empty,
            ShiftId = shiftId ?? Guid.Empty,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        await PopulateAssignDataAsync(model, cancellationToken);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignShiftViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAssignDataAsync(model, cancellationToken);
            return View(model);
        }

        var command = new AssignShiftCommand(
            model.EmployeeId,
            model.ShiftId,
            model.EffectiveFrom,
            model.EffectiveTo
        );

        var result = await _shiftService.AssignShiftAsync(command, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await PopulateAssignDataAsync(model, cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = "Employee shift assigned successfully.";
        return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
    }

    private async Task PopulateAssignDataAsync(AssignShiftViewModel model, CancellationToken cancellationToken = default)
    {
        model.AvailableShifts = await _shiftService.GetShiftsAsync(ShiftStatus.Active, cancellationToken);

        model.AvailableDepartments = await _dbContext.Departments
            .AsNoTracking()
            .Where(d => d.Status == EntityStatus.Active)
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentOptionViewModel
            {
                DepartmentId = d.DepartmentId,
                Name = d.Name
            })
            .ToListAsync(cancellationToken);

        var employees = await (
            from e in _dbContext.Employees.AsNoTracking()
            where e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnNotice
            join d in _dbContext.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId into deptGroup
            from d in deptGroup.DefaultIfEmpty()
            orderby e.FirstName, e.LastName
            select new AssignEmployeeItemViewModel
            {
                EmployeeId = e.EmployeeId,
                EmployeeCode = e.EmployeeCode,
                FullName = (e.FirstName + " " + e.LastName).Trim(),
                DepartmentId = e.DepartmentId,
                DepartmentName = d != null ? d.Name : "General"
            }
        ).ToListAsync(cancellationToken);

        model.AvailableEmployees = employees;

        if (model.EmployeeId != Guid.Empty)
        {
            var selected = employees.FirstOrDefault(e => e.EmployeeId == model.EmployeeId);
            if (selected != null)
            {
                model.SelectedEmployeeName = selected.FullName;
                model.SelectedEmployeeCode = selected.EmployeeCode;
                model.SelectedEmployeeDept = selected.DepartmentName;
                model.SelectedDepartmentId = selected.DepartmentId;
            }
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var assignment = await _shiftService.GetAssignmentByIdAsync(id);
        if (assignment == null)
        {
            TempData["ErrorMessage"] = "Shift assignment not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EditShiftAssignmentViewModel
        {
            ShiftAssignmentId = assignment.ShiftAssignmentId,
            EmployeeId = assignment.EmployeeId,
            EmployeeName = $"{assignment.EmployeeName} ({assignment.EmployeeCode})",
            ShiftName = assignment.ShiftName,
            EffectiveFrom = assignment.EffectiveFrom,
            EffectiveTo = assignment.EffectiveTo
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditShiftAssignmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _shiftService.UpdateAssignmentAsync(model.ShiftAssignmentId, model.EffectiveFrom, model.EffectiveTo);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Shift assignment updated successfully.";
        return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
    }

    [HttpGet]
    public async Task<IActionResult> Transfer(Guid employeeId)
    {
        var emp = await _employeeLookup.GetBasicInfoAsync(employeeId);
        if (emp == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction(nameof(Index));
        }

        var currentAssignments = await _shiftService.GetAssignmentsAsync(employeeId: employeeId);
        var current = currentAssignments.FirstOrDefault(a => a.IsCurrent);

        if (current == null)
        {
            TempData["ErrorMessage"] = "Employee has no active shift to transfer from. Please assign a shift first.";
            return RedirectToAction(nameof(Assign), new { employeeId });
        }

        var shifts = await _shiftService.GetShiftsAsync(ShiftStatus.Active);

        var model = new TransferShiftViewModel
        {
            EmployeeId = employeeId,
            EmployeeName = $"{emp.FullName} ({emp.EmployeeCode})",
            CurrentShiftName = current.ShiftName,
            CurrentShiftFrom = current.EffectiveFrom,
            NewShiftId = Guid.Empty,
            EffectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            AvailableShifts = shifts.Where(s => s.ShiftId != current.ShiftId).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(TransferShiftViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableShifts = await _shiftService.GetShiftsAsync(ShiftStatus.Active);
            return View(model);
        }

        var command = new TransferShiftCommand(
            model.EmployeeId,
            model.NewShiftId,
            model.EffectiveFrom
        );

        var result = await _shiftService.TransferShiftAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            model.AvailableShifts = await _shiftService.GetShiftsAsync(ShiftStatus.Active);
            return View(model);
        }

        TempData["SuccessMessage"] = "Employee shift transferred successfully.";
        return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> End(Guid shiftAssignmentId, DateOnly effectiveTo)
    {
        var command = new EndShiftAssignmentCommand(shiftAssignmentId, effectiveTo);
        var result = await _shiftService.EndAssignmentAsync(command);

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Shift assignment closed successfully.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _shiftService.DeleteAssignmentAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Employee shift assignment deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
