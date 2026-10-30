using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Domain.Enums;

namespace Hrms.Web.Areas.Attendance.Models;

public class DailySheetViewModel
{
    public DateOnly WorkDate { get; set; }
    public bool IsHoliday { get; set; }
    public string? HolidayName { get; set; }
    public bool IsWeeklyOff { get; set; }
    public bool CanManage { get; set; }
    public List<DailySheetRowViewModel> Items { get; set; } = new();
}

public class DailySheetRowViewModel
{
    public Guid EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Guid? AttendanceDayId { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public int LateMinutes { get; set; }
    public int WorkedMinutes { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Note { get; set; }
    public string? ShiftName { get; set; }
    public TimeSpan? ShiftStartTime { get; set; }
    public TimeSpan? ShiftEndTime { get; set; }
    public bool IsReadonly { get; set; }
    public string? RowVersionBase64 { get; set; }
}

public class SaveDailySheetInputModel
{
    public DateOnly WorkDate { get; set; }
    public List<DailySheetItemInputModel> Items { get; set; } = new();
}

public class DailySheetItemInputModel
{
    public Guid EmployeeId { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public string? Note { get; set; }
}

public class UpdateAttendanceRecordInputModel
{
    public Guid AttendanceDayId { get; set; }
    public DateOnly WorkDate { get; set; }
    public TimeOnly? CheckInTime { get; set; }
    public TimeOnly? CheckOutTime { get; set; }
    public string Note { get; set; } = string.Empty;
    public string RowVersion { get; set; } = string.Empty;
}

public class MonthlyAttendanceViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int DaysInMonth { get; set; }
    public string? SearchTerm { get; set; }
    public IReadOnlyList<MonthlyEmployeeAttendanceDto> Employees { get; set; } = new List<MonthlyEmployeeAttendanceDto>();
}

public class MyAttendanceViewModel
{
    public bool HasEmployee { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public EmployeeMonthlyReportDto? Report { get; set; }
}
