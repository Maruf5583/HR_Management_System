using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Enums;
using Hrms.Identity.Application.Models;
using Hrms.Identity.Application.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Employees.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Employees.Controllers;

[Area("Employees")]
public class EmployeesController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IDepartmentService _departmentService;
    private readonly IDesignationService _designationService;
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;
    private readonly ICurrentUser _currentUser;

    public EmployeesController(
        IEmployeeService employeeService,
        IDepartmentService departmentService,
        IDesignationService designationService,
        IUserService userService,
        IRoleService roleService,
        ICurrentUser currentUser)
    {
        _employeeService = employeeService;
        _departmentService = departmentService;
        _designationService = designationService;
        _userService = userService;
        _roleService = roleService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.View)]
    public async Task<IActionResult> Index(EmployeeFilterDto filter)
    {
        var paged = await _employeeService.GetEmployeesAsync(filter);
        var departments = await _departmentService.GetDepartmentsAsync();
        var designations = await _designationService.GetDesignationsAsync(filter.DepartmentId);

        var viewModel = new EmployeeIndexViewModel
        {
            Filter = filter,
            PagedEmployees = paged,
            DepartmentOptions = departments.Where(d => d.Status == EntityStatus.Active)
                .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == filter.DepartmentId)),
            DesignationOptions = designations.Where(d => d.Status == EntityStatus.Active)
                .Select(d => new SelectListItem(d.Title, d.Id.ToString(), d.Id == filter.DesignationId))
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.View)]
    public async Task<IActionResult> Details(Guid id)
    {
        var canViewNid = _currentUser.HasPermission(Permissions.Employees.ViewNid);
        var employee = await _employeeService.GetEmployeeDetailsAsync(id, canViewNid);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(employee);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.ViewOwn)]
    public async Task<IActionResult> MyProfile()
    {
        if (!_currentUser.EmployeeId.HasValue || _currentUser.EmployeeId.Value == Guid.Empty)
        {
            return View("NoLinkedEmployee");
        }

        var employee = await _employeeService.GetMyProfileAsync(_currentUser.EmployeeId.Value);
        if (employee == null)
        {
            return View("NoLinkedEmployee");
        }

        return View(employee);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.Manage)]
    public async Task<IActionResult> Create()
    {
        var departments = await _departmentService.GetDepartmentsAsync();
        var model = new EmployeeFormViewModel
        {
            DepartmentOptions = departments.Where(d => d.Status == EntityStatus.Active)
                .Select(d => new SelectListItem(d.Name, d.Id.ToString())),
            CanViewNid = _currentUser.HasPermission(Permissions.Employees.ViewNid),
            CreateUserAccount = true,
            UserRole = "Employee"
        };

        await PopulateRolesDropdownAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Employees.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model)
    {
        if (model.CreateUserAccount)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Work email is required to create a login account.");
            }
            if (string.IsNullOrWhiteSpace(model.Password))
            {
                ModelState.AddModelError(nameof(model.Password), "Password is required to create a login account.");
            }
            else if (model.Password.Length < 6)
            {
                ModelState.AddModelError(nameof(model.Password), "Password must be at least 6 characters long.");
            }
            else if (model.Password != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Password and confirmation password do not match.");
            }
        }

        if (model.Photo != null && model.Photo.Length > 0)
        {
            if (model.Photo.Length > 1_048_576)
            {
                ModelState.AddModelError(nameof(model.Photo), "Photo file size cannot exceed 1 MB.");
            }
            var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
            {
                ModelState.AddModelError(nameof(model.Photo), "Only JPG, JPEG, or PNG image files are allowed.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            await PopulateRolesDropdownAsync(model);
            return View(model);
        }

        var command = new CreateEmployeeModel(
            model.FirstName,
            model.LastName,
            model.Email,
            model.Phone,
            model.DateOfBirth,
            model.Gender,
            model.Address,
            _currentUser.HasPermission(Permissions.Employees.ViewNid) ? model.NidNumber : null,
            model.JoinDate,
            model.EmploymentType,
            model.DepartmentId,
            model.DesignationId,
            model.GradeId,
            model.EmergencyContactName,
            model.EmergencyContactPhone
        );

        var result = await _employeeService.CreateEmployeeAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await PopulateDropdownsAsync(model);
            await PopulateRolesDropdownAsync(model);
            return View(model);
        }

        var employeeId = result.Value;

        // Upload photo if provided
        if (model.Photo != null && model.Photo.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await model.Photo.CopyToAsync(memoryStream);
            var photoBytes = memoryStream.ToArray();
            await _employeeService.UploadPhotoAsync(new UploadPhotoModel(
                employeeId,
                model.Photo.FileName,
                photoBytes
            ));
        }

        // Create user login account if requested
        if (model.CreateUserAccount && !string.IsNullOrWhiteSpace(model.Email) && !string.IsNullOrWhiteSpace(model.Password))
        {
            var roleName = string.IsNullOrWhiteSpace(model.UserRole) ? "Employee" : model.UserRole;
            if (!_currentUser.HasPermission(Permissions.Users.Manage))
            {
                roleName = "Employee";
            }

            var userResult = await _userService.CreateUserAsync(new CreateUserModel(
                model.Email.Trim(),
                model.Password,
                roleName,
                employeeId
            ));

            if (!userResult.IsSuccess)
            {
                TempData["WarningMessage"] = $"Employee created, but login user could not be created: {userResult.Error.Message}";
                return RedirectToAction(nameof(Details), new { id = employeeId });
            }

            TempData["SuccessMessage"] = $"Employee '{model.FirstName} {model.LastName}' and user account '{model.Email}' created successfully. The employee can now sign in.";
            return RedirectToAction(nameof(Details), new { id = employeeId });
        }

        TempData["SuccessMessage"] = $"Employee '{model.FirstName} {model.LastName}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = employeeId });
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.Manage)]
    public async Task<IActionResult> Edit(Guid id)
    {
        var canViewNid = _currentUser.HasPermission(Permissions.Employees.ViewNid);
        var employee = await _employeeService.GetEmployeeDetailsAsync(id, canViewNid);
        if (employee == null)
        {
            TempData["ErrorMessage"] = "Employee not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EmployeeFormViewModel
        {
            EmployeeId = employee.EmployeeId,
            EmployeeCode = employee.EmployeeCode,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            Phone = employee.Phone,
            DateOfBirth = employee.DateOfBirth,
            Gender = employee.Gender,
            Address = employee.Address,
            NidNumber = employee.NidNumber,
            JoinDate = employee.JoinDate,
            EmploymentType = employee.EmploymentType,
            DepartmentId = employee.DepartmentId,
            DesignationId = employee.DesignationId,
            GradeId = employee.GradeId,
            EmergencyContactName = employee.EmergencyContactName,
            EmergencyContactPhone = employee.EmergencyContactPhone,
            RowVersion = employee.RowVersion,
            CanViewNid = canViewNid,
            CurrentPhotoKey = employee.PhotoKey,
            CreateUserAccount = false
        };

        await PopulateDropdownsAsync(model);
        return View(model);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Employees.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmployeeFormViewModel model)
    {
        if (model.RowVersion == null || model.RowVersion.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Concurrency token is missing. Please refresh and try again.");
        }

        if (model.Photo != null && model.Photo.Length > 0)
        {
            if (model.Photo.Length > 1_048_576)
            {
                ModelState.AddModelError(nameof(model.Photo), "Photo file size cannot exceed 1 MB.");
            }
            var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png"))
            {
                ModelState.AddModelError(nameof(model.Photo), "Only JPG, JPEG, or PNG image files are allowed.");
            }
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var canViewNid = _currentUser.HasPermission(Permissions.Employees.ViewNid);
        var command = new UpdateEmployeeModel(
            id,
            model.FirstName,
            model.LastName,
            model.Email,
            model.Phone,
            model.DateOfBirth,
            model.Gender,
            model.Address,
            canViewNid ? model.NidNumber : null,
            model.JoinDate,
            model.EmploymentType,
            model.DepartmentId,
            model.DesignationId,
            model.GradeId,
            model.EmergencyContactName,
            model.EmergencyContactPhone,
            model.RowVersion!
        );

        var result = await _employeeService.UpdateEmployeeAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        if (model.Photo != null && model.Photo.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await model.Photo.CopyToAsync(memoryStream);
            var photoBytes = memoryStream.ToArray();
            var photoResult = await _employeeService.UploadPhotoAsync(new UploadPhotoModel(
                id,
                model.Photo.FileName,
                photoBytes
            ));
            if (!photoResult.IsSuccess)
            {
                TempData["WarningMessage"] = photoResult.Error.Message;
            }
        }

        TempData["SuccessMessage"] = $"Employee '{model.FirstName} {model.LastName}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [Authorize(Policy = Permissions.Employees.Manage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(ChangeStatusViewModel model)
    {
        var command = new ChangeEmployeeStatusModel(
            model.EmployeeId,
            model.NewStatus,
            model.ExitDate,
            model.RowVersion
        );

        var result = await _employeeService.ChangeStatusAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Status updated to {model.NewStatus}.";
        }

        return RedirectToAction(nameof(Details), new { id = model.EmployeeId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1_200_000)] // ~1.2 MB upload limit
    public async Task<IActionResult> UploadPhoto(UploadPhotoViewModel model)
    {
        var canManage = _currentUser.HasPermission(Permissions.Employees.Manage);
        var isOwnProfile = _currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == model.EmployeeId;

        if (!canManage && !isOwnProfile)
        {
            return Forbid();
        }

        if (model.Photo == null || model.Photo.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a photo file.";
            return isOwnProfile && !canManage 
                ? RedirectToAction(nameof(MyProfile)) 
                : RedirectToAction(nameof(Details), new { id = model.EmployeeId });
        }

        using var memoryStream = new MemoryStream();
        await model.Photo.CopyToAsync(memoryStream);
        var photoBytes = memoryStream.ToArray();

        var result = await _employeeService.UploadPhotoAsync(new UploadPhotoModel(
            model.EmployeeId,
            model.Photo.FileName,
            photoBytes
        ));

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Profile photo updated successfully.";
        }

        return isOwnProfile && !canManage 
            ? RedirectToAction(nameof(MyProfile)) 
            : RedirectToAction(nameof(Details), new { id = model.EmployeeId });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemovePhoto(Guid id)
    {
        var canManage = _currentUser.HasPermission(Permissions.Employees.Manage);
        var isOwnProfile = _currentUser.EmployeeId.HasValue && _currentUser.EmployeeId.Value == id;

        if (!canManage && !isOwnProfile)
        {
            return Forbid();
        }

        var result = await _employeeService.RemovePhotoAsync(id);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Profile photo removed.";
        }

        return isOwnProfile && !canManage 
            ? RedirectToAction(nameof(MyProfile)) 
            : RedirectToAction(nameof(Details), new { id });
    }

    // AJAX Cascading Dropdown Endpoints
    [HttpGet]
    [Authorize(Policy = Permissions.Employees.View)]
    public async Task<IActionResult> GetDesignations(Guid departmentId)
    {
        var designations = await _employeeService.GetActiveDesignationsDropdownAsync(departmentId);
        return Json(designations);
    }

    [HttpGet]
    [Authorize(Policy = Permissions.Employees.View)]
    public async Task<IActionResult> GetGrades(Guid designationId)
    {
        var grades = await _employeeService.GetActiveGradesDropdownAsync(designationId);
        return Json(grades);
    }

    private async Task PopulateRolesDropdownAsync(EmployeeFormViewModel model)
    {
        if (_currentUser.HasPermission(Permissions.Users.Manage))
        {
            var roles = await _roleService.GetRolesAsync();
            model.RoleOptions = roles.Select(r => new SelectListItem(r.Name, r.Name, r.Name == model.UserRole));
        }
        else
        {
            model.RoleOptions = new List<SelectListItem>
            {
                new SelectListItem("Employee", "Employee", true)
            };
        }
    }

    private async Task PopulateDropdownsAsync(EmployeeFormViewModel model)
    {
        var departments = await _departmentService.GetDepartmentsAsync();
        model.DepartmentOptions = departments.Where(d => d.Status == EntityStatus.Active)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == model.DepartmentId));

        if (model.DepartmentId != Guid.Empty)
        {
            var designations = await _employeeService.GetActiveDesignationsDropdownAsync(model.DepartmentId);
            model.DesignationOptions = designations
                .Select(d => new SelectListItem(d.Name, d.Id.ToString(), d.Id == model.DesignationId));
        }

        if (model.DesignationId != Guid.Empty)
        {
            var grades = await _employeeService.GetActiveGradesDropdownAsync(model.DesignationId);
            model.GradeOptions = grades
                .Select(g => new SelectListItem(g.Name, g.Id.ToString(), g.Id == model.GradeId));
        }

        model.CanViewNid = _currentUser.HasPermission(Permissions.Employees.ViewNid);
    }
}
