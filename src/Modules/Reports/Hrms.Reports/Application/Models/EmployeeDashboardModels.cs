using Hrms.Attendance.Domain.Enums;

namespace Hrms.Reports.Application.Models;

public record EmployeeDashboardDto(
    Guid EmployeeId,
    string FullName,
    string EmployeeCode,
    string DepartmentName,
    string DesignationTitle,
    string AvatarText,
    string CurrentTimeFormatted,
    string TodayDateFormatted,
    EmployeeTodayAttendanceDto Today,
    EmployeeShiftOverviewDto ShiftOverview,
    EmployeeLeaveSummaryDto LeaveSummary,
    IReadOnlyList<EmployeeTeamMemberDto> TeamMembers,
    EmployeeCalendarDto Calendar,
    IReadOnlyList<EmployeeRecentAttendanceDto> WorkingHistory
);

public record EmployeeTodayAttendanceDto(
    AttendanceStatus? Status,
    string StatusText,
    string? CheckInFormatted,
    string? CheckOutFormatted,
    int WorkedMinutes,
    string WorkedHoursFormatted,
    string TimeLeftFormatted,
    int ProgressPercent,
    bool IsCheckedIn,
    bool IsCheckedOut,
    string Note
);

public record EmployeeShiftOverviewDto(
    string ShiftName,
    string TimingFormatted,
    string AverageHoursFormatted,
    string AverageCheckInFormatted,
    string AverageCheckOutFormatted,
    decimal OnTimeArrivalRate,
    int DaysPresentThisMonth,
    int TotalWorkingDaysThisMonth,
    string ComparisonNote
);

public record EmployeeLeaveSummaryDto(
    decimal TotalEntitled,
    decimal TotalUsed,
    decimal TotalRemaining,
    int RemainingPercentage,
    IReadOnlyList<EmployeeLeaveTypeBalanceDto> Balances,
    int PendingRequestsCount
);

public record EmployeeLeaveTypeBalanceDto(
    string Name,
    string Code,
    decimal Entitled,
    decimal Used,
    decimal Remaining,
    string ColorClass
);

public record EmployeeTeamMemberDto(
    Guid EmployeeId,
    string FullName,
    string Designation,
    string Initial,
    AttendanceStatus? TodayStatus,
    string TodayStatusText,
    string BadgeClass,
    IReadOnlyDictionary<int, string> WeeklyStatusCodes
);

public record EmployeeCalendarDto(
    int Year,
    int Month,
    string MonthName,
    int TotalDaysInMonth,
    int FirstDayOfWeekOffset,
    IReadOnlyList<DayOfWeek> WeeklyOffDays,
    IReadOnlyDictionary<DateOnly, string> Holidays,
    IReadOnlyDictionary<DateOnly, EmployeeCalendarDayDto> AttendanceByDate
);

public record EmployeeCalendarDayDto(
    DateOnly Date,
    AttendanceStatus Status,
    string StatusText,
    string? CheckIn,
    string? CheckOut,
    string WorkedHours,
    bool IsToday,
    bool IsWeekend,
    bool IsHoliday,
    string? HolidayName
);

public record EmployeeRecentAttendanceDto(
    DateOnly Date,
    string DateFormatted,
    string CheckInFormatted,
    string CheckOutFormatted,
    string EffectiveTimeFormatted,
    string TargetComparisonText,
    bool IsOnTarget,
    AttendanceStatus Status,
    string StatusText
);
