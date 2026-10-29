using System.ComponentModel.DataAnnotations;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Domain.Enums;

namespace Hrms.Web.Areas.Calendar.Models;

public class ShiftListViewModel
{
    public IReadOnlyList<ShiftDto> Shifts { get; set; } = new List<ShiftDto>();
    public ShiftStatus? StatusFilter { get; set; }
    public int TotalCount { get; set; }
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
}

public class ShiftCreateViewModel
{
    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(100, ErrorMessage = "Shift name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [Required(ErrorMessage = "End time is required")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);

    [Range(0, 360, ErrorMessage = "Grace minutes must be between 0 and 360")]
    public int GraceMinutes { get; set; } = 15;

    [Range(0, 360, ErrorMessage = "Break minutes must be between 0 and 360")]
    public int BreakMinutes { get; set; } = 60;

    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public class ShiftEditViewModel
{
    public Guid ShiftId { get; set; }

    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(100, ErrorMessage = "Shift name must not exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    public ShiftStatus Status { get; set; }
}

public class ShiftVersionCreateViewModel
{
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);

    [Required(ErrorMessage = "End time is required")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(18, 0, 0);

    [Range(0, 360, ErrorMessage = "Grace minutes must be between 0 and 360")]
    public int GraceMinutes { get; set; } = 15;

    [Range(0, 360, ErrorMessage = "Break minutes must be between 0 and 360")]
    public int BreakMinutes { get; set; } = 60;

    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}

public class ShiftDetailsViewModel
{
    public ShiftDetailsDto Details { get; set; } = null!;
    public IReadOnlyList<ShiftAssignmentDto> RecentAssignments { get; set; } = new List<ShiftAssignmentDto>();
}
