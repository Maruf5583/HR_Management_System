using System.ComponentModel.DataAnnotations;
using Hrms.Identity.Domain.Enums;

namespace Hrms.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string? CurrentPassword { get; set; }

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Initial password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role selection is required.")]
    public string RoleName { get; set; } = string.Empty;

    [Display(Name = "Linked Employee")]
    public Guid? EmployeeId { get; set; }
}

public class EditUserViewModel
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role selection is required.")]
    public string RoleName { get; set; } = string.Empty;

    public UserStatus Status { get; set; }

    [Display(Name = "Linked Employee")]
    public Guid? EmployeeId { get; set; }
}

public class ResetPasswordViewModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
}

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Role name is required.")]
    [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> SelectedPermissions { get; set; } = new();
}

public class EditRoleViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsAdminRole => string.Equals(Name, "Admin", StringComparison.OrdinalIgnoreCase);

    public List<string> SelectedPermissions { get; set; } = new();
}

public class EmptyStateViewModel
{
    public string Message { get; set; } = "No records found.";
    public string? Subtitle { get; set; }
    public string Icon { get; set; } = "bi-inbox";
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
    public string? ActionPermission { get; set; }
}

public class PaginationViewModel
{
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; } = 0;
    public int PageSize { get; set; } = 10;
    public string BaseUrl { get; set; } = string.Empty;
}
