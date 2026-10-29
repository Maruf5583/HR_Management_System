using System.ComponentModel.DataAnnotations;
using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Domain.Enums;

namespace Hrms.Web.Areas.Calendar.Models;

public class HolidayListViewModel
{
    public IReadOnlyList<HolidayDto> Holidays { get; set; } = new List<HolidayDto>();
    public IReadOnlyList<CompanyWorkingDayDto> WorkingDays { get; set; } = new List<CompanyWorkingDayDto>();
    public int SelectedYear { get; set; }
    public HolidayType? SelectedType { get; set; }
    public IReadOnlyList<int> AvailableYears { get; set; } = new List<int>();
    public int ViewMonth { get; set; }
    public int ViewYear { get; set; }

    public bool IsWeekend(DayOfWeek dayOfWeek)
    {
        var match = WorkingDays.FirstOrDefault(w => w.DayOfWeek == (byte)dayOfWeek);
        if (match != null)
        {
            return !match.IsWorkingDay;
        }
        return dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday;
    }
}

public class HolidayFormViewModel
{
    public Guid? HolidayId { get; set; }

    [Required(ErrorMessage = "Holiday name is required")]
    [StringLength(120, ErrorMessage = "Holiday name must not exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Holiday date is required")]
    public DateOnly HolidayDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required(ErrorMessage = "Holiday type is required")]
    public HolidayType Type { get; set; } = HolidayType.Government;
}

public class MultiDayHolidayFormViewModel
{
    [Required(ErrorMessage = "Holiday / Vacation name is required")]
    [StringLength(120, ErrorMessage = "Holiday name must not exceed 120 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);

    [Required(ErrorMessage = "End date is required")]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

    [Required(ErrorMessage = "Holiday type is required")]
    public HolidayType Type { get; set; } = HolidayType.Government;
}
