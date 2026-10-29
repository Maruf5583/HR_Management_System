namespace Hrms.Web.Areas.Calendar.Models;

public class WorkingDayItemViewModel
{
    public byte DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsWorkingDay { get; set; }
}

public class WorkingDaysFormViewModel
{
    public List<WorkingDayItemViewModel> Days { get; set; } = new();
}
