using System.ComponentModel.DataAnnotations;
using Hrms.Calendar.Application.Models;
using Hrms.Employees.Contracts;

namespace Hrms.Web.Areas.Calendar.Models;

public class ShiftAssignmentListViewModel
{
    public IReadOnlyList<ShiftAssignmentDto> Assignments { get; set; } = new List<ShiftAssignmentDto>();
    public Guid? SelectedShiftId { get; set; }
    public Guid? SelectedEmployeeId { get; set; }
    public IReadOnlyList<ShiftDto> Shifts { get; set; } = new List<ShiftDto>();
    public IReadOnlyList<EmployeeLookupDto> Employees { get; set; } = new List<EmployeeLookupDto>();
}

public class DepartmentOptionViewModel
{
    public Guid DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AssignEmployeeItemViewModel
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}

public class AssignShiftViewModel
{
    [Required(ErrorMessage = "Employee is required")]
    public Guid EmployeeId { get; set; }

    public Guid? SelectedDepartmentId { get; set; }

    [Required(ErrorMessage = "Shift is required")]
    public Guid ShiftId { get; set; }

    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    public DateOnly? EffectiveTo { get; set; }

    public IReadOnlyList<ShiftDto> AvailableShifts { get; set; } = new List<ShiftDto>();
    public IReadOnlyList<DepartmentOptionViewModel> AvailableDepartments { get; set; } = new List<DepartmentOptionViewModel>();
    public IReadOnlyList<AssignEmployeeItemViewModel> AvailableEmployees { get; set; } = new List<AssignEmployeeItemViewModel>();

    public string SelectedEmployeeName { get; set; } = string.Empty;
    public string SelectedEmployeeCode { get; set; } = string.Empty;
    public string SelectedEmployeeDept { get; set; } = string.Empty;
}

public class TransferShiftViewModel
{
    [Required(ErrorMessage = "Employee is required")]
    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;
    public string CurrentShiftName { get; set; } = string.Empty;
    public DateOnly CurrentShiftFrom { get; set; }

    [Required(ErrorMessage = "New shift is required")]
    public Guid NewShiftId { get; set; }

    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    public IReadOnlyList<ShiftDto> AvailableShifts { get; set; } = new List<ShiftDto>();
}

public class EditShiftAssignmentViewModel
{
    public Guid ShiftAssignmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string ShiftName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; }

    public DateOnly? EffectiveTo { get; set; }
}
