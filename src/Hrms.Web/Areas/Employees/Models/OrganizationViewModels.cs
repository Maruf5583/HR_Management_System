using System.ComponentModel.DataAnnotations;
using Hrms.Employees.Application.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Employees.Models;

public class DepartmentFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Department name is required.")]
    [StringLength(100, ErrorMessage = "Department name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}

public class DesignationFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Designation title is required.")]
    [StringLength(120, ErrorMessage = "Designation title cannot exceed 120 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    public Guid DepartmentId { get; set; }

    public IEnumerable<SelectListItem> DepartmentOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}

public class GradeFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Designation is required.")]
    public Guid DesignationId { get; set; }

    [Required(ErrorMessage = "Level is required.")]
    [StringLength(30, ErrorMessage = "Level cannot exceed 30 characters.")]
    public string Level { get; set; } = string.Empty;

    [Required(ErrorMessage = "Grade code is required.")]
    [StringLength(30, ErrorMessage = "Grade code cannot exceed 30 characters.")]
    public string Code { get; set; } = string.Empty;

    [Range(0, 999999999.99, ErrorMessage = "Salary must be greater than or equal to 0.")]
    public decimal Salary { get; set; }

    public IEnumerable<SelectListItem> DesignationOptions { get; set; } = Enumerable.Empty<SelectListItem>();
}
