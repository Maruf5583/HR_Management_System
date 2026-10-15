using System.ComponentModel.DataAnnotations;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Employees.Models;

public class EmployeeIndexViewModel
{
    public EmployeeFilterDto Filter { get; set; } = new();
    public PagedResult<EmployeeListItemDto> PagedEmployees { get; set; } = new(Array.Empty<EmployeeListItemDto>(), 0, 1, 10);
    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DesignationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class EmployeeFormViewModel
{
    public Guid? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }

    [Required(ErrorMessage = "First name is required.")]
    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters.")]
    public string LastName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format.")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters.")]
    public string? Email { get; set; }

    [StringLength(30, ErrorMessage = "Phone cannot exceed 30 characters.")]
    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    [StringLength(20, ErrorMessage = "Gender cannot exceed 20 characters.")]
    public string? Gender { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters.")]
    public string? Address { get; set; }

    [StringLength(50, ErrorMessage = "NID cannot exceed 50 characters.")]
    public string? NidNumber { get; set; }

    [Required(ErrorMessage = "Join date is required.")]
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Employment type is required.")]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    [Required(ErrorMessage = "Department is required.")]
    public Guid DepartmentId { get; set; }

    [Required(ErrorMessage = "Designation is required.")]
    public Guid DesignationId { get; set; }

    [Required(ErrorMessage = "Grade is required.")]
    public Guid GradeId { get; set; }

    [StringLength(150, ErrorMessage = "Emergency contact name cannot exceed 150 characters.")]
    public string? EmergencyContactName { get; set; }

    [StringLength(30, ErrorMessage = "Emergency contact phone cannot exceed 30 characters.")]
    public string? EmergencyContactPhone { get; set; }

    public byte[]? RowVersion { get; set; }
    public bool CanViewNid { get; set; }
    public string? CurrentPhotoKey { get; set; }

    // User Account & Login Credentials
    public bool CreateUserAccount { get; set; } = true;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }

    public string UserRole { get; set; } = "Employee";
    public IEnumerable<SelectListItem> RoleOptions { get; set; } = Enumerable.Empty<SelectListItem>();

    // Profile Photo
    public IFormFile? Photo { get; set; }

    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> DesignationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> GradeOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class ChangeStatusViewModel
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public EmployeeStatus CurrentStatus { get; set; }
    public EmployeeStatus NewStatus { get; set; }
    public DateOnly? ExitDate { get; set; }
    public DateOnly JoinDate { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class UploadPhotoViewModel
{
    [Required]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Please select a photo to upload.")]
    public IFormFile? Photo { get; set; }
}
