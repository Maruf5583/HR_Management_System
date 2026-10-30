using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Domain.Enums;
using Hrms.Leave.Domain.Enums;

namespace Hrms.Reports.Application.Models;

// ==========================================
// 1. Dashboard Models
// ==========================================
public record DashboardSummaryDto(
    int TotalActiveEmployees,
    int TotalDepartments,
    int TotalAssignedAssets,
    int TotalAvailableAssets,
    int PendingLeaveApprovals,
    DateOnly TodayDate,
    bool TodayIsHoliday,
    string? TodayHolidayName,
    bool TodayIsWeekend,
    int TodayPresent,
    int TodayLate,
    int TodayHalfDay,
    int TodayAbsent,
    int TodayOnLeave,
    int TodayWeekendOrHoliday,
    IReadOnlyList<UpcomingHolidayDto> UpcomingHolidays,
    IReadOnlyList<RecentAttendanceItemDto> RecentAttendances
);

public record UpcomingHolidayDto(
    DateOnly HolidayDate,
    string Name,
    string Type,
    int DaysUntil
);

public record RecentAttendanceItemDto(
    string EmployeeCode,
    string FullName,
    string Department,
    TimeOnly? CheckIn,
    TimeOnly? CheckOut,
    AttendanceStatus Status,
    int LateMinutes
);

// ==========================================
// 2. Attendance Report Models
// ==========================================
public record AttendanceReportFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? DepartmentId,
    Guid? EmployeeId,
    AttendanceStatus? Status
);

public record AttendanceReportItemDto(
    DateOnly WorkDate,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string DesignationTitle,
    string? ShiftName,
    TimeOnly? CheckInTime,
    TimeOnly? CheckOutTime,
    int LateMinutes,
    int WorkedMinutes,
    AttendanceStatus Status,
    string? Note
);

public record AttendanceReportSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int TotalRecords,
    int PresentCount,
    int LateCount,
    int HalfDayCount,
    int AbsentCount,
    int OnLeaveCount,
    int HolidayCount,
    int WeekendCount,
    IReadOnlyList<AttendanceReportItemDto> Items
);

// ==========================================
// 3. Leave Report Models
// ==========================================
public record LeaveBalanceReportFilter(
    int? Year,
    Guid? DepartmentId,
    Guid? LeaveTypeId
);

public record LeaveBalanceReportItemDto(
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string LeaveTypeName,
    int Year,
    decimal Entitled,
    decimal Used,
    decimal Remaining
);

public record LeaveRecordReportFilter(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? DepartmentId,
    Guid? LeaveTypeId,
    LeaveRecordStatus? Status
);

public record LeaveRecordReportItemDto(
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string LeaveTypeName,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal TotalDays,
    bool IsHalfDay,
    LeaveRecordStatus Status,
    string? Reason,
    DateTime CreatedAtUtc
);

// ==========================================
// 4. Employee Report Models
// ==========================================
public record EmployeeReportFilter(
    Guid? DepartmentId,
    Guid? DesignationId,
    EmploymentType? EmploymentType,
    EmployeeStatus? Status
);

public record EmployeeReportItemDto(
    string EmployeeCode,
    string FullName,
    string Email,
    string Phone,
    DateOnly? DateOfBirth,
    string? Gender,
    string DepartmentName,
    string DesignationTitle,
    string GradeCode,
    decimal Salary,
    DateOnly JoinDate,
    EmploymentType EmploymentType,
    EmployeeStatus Status,
    DateOnly? ExitDate
);

// ==========================================
// 5. Asset Report Models
// ==========================================
public record AssetReportFilter(
    Assets.Domain.Enums.AssetCategory? Category,
    Assets.Domain.Enums.AssetStatus? Status,
    Guid? DepartmentId
);

public record AssetReportItemDto(
    string AssetTag,
    string Name,
    Assets.Domain.Enums.AssetCategory Category,
    string? SerialNumber,
    DateOnly? PurchaseDate,
    Assets.Domain.Enums.AssetCondition Condition,
    Assets.Domain.Enums.AssetStatus Status,
    string? AssignedToEmployeeCode,
    string? AssignedToFullName,
    string? AssignedToDepartment,
    DateTime? AssignedAtUtc
);
