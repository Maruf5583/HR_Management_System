using Hrms.Attendance.Domain.Enums;

namespace Hrms.Attendance.Application.Models;

public record DailySheetItemDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    Guid? AttendanceDayId,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    int LateMinutes,
    int WorkedMinutes,
    AttendanceStatus Status,
    string? Note,
    string? ShiftName,
    TimeSpan? ShiftStartTime,
    TimeSpan? ShiftEndTime,
    Guid? ShiftVersionId,
    bool IsReadonly, // true for weekends, holidays, approved leave
    byte[]? RowVersion
);

public record DailySheetDto(
    DateOnly WorkDate,
    bool IsHoliday,
    string? HolidayName,
    bool IsWeeklyOff,
    IReadOnlyList<DailySheetItemDto> Items
);

public record DailySheetItemInput(
    Guid EmployeeId,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    string? Note
);

public record AttendanceRecordDto(
    Guid AttendanceDayId,
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    DateOnly WorkDate,
    DateTime? CheckInUtc,
    DateTime? CheckOutUtc,
    TimeOnly? CheckInLocal,
    TimeOnly? CheckOutLocal,
    int LateMinutes,
    int WorkedMinutes,
    AttendanceStatus Status,
    string? Note,
    string? ShiftName,
    byte[] RowVersion
);

public record MonthlyEmployeeAttendanceDto(
    Guid EmployeeId,
    string EmployeeCode,
    string FullName,
    IReadOnlyDictionary<int, AttendanceStatus?> DailyStatuses,
    int PresentCount,
    int LateCount,
    int HalfDayCount,
    int AbsentCount,
    int OnLeaveCount,
    int HolidayCount,
    int WeekendCount
);

public record MonthlyAttendanceDto(
    int Year,
    int Month,
    int DaysInMonth,
    IReadOnlyList<MonthlyEmployeeAttendanceDto> Employees
);

public record EmployeeAttendanceDayDto(
    DateOnly WorkDate,
    string DayOfWeek,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    int LateMinutes,
    int WorkedMinutes,
    AttendanceStatus Status,
    string? Note,
    string? ShiftName
);

public record EmployeeMonthlyReportDto(
    Guid EmployeeId,
    int Year,
    int Month,
    int PresentDays,
    int LateDays,
    int HalfDays,
    int AbsentDays,
    int OnLeaveDays,
    int HolidayDays,
    int WeekendDays,
    IReadOnlyList<EmployeeAttendanceDayDto> Days
);
