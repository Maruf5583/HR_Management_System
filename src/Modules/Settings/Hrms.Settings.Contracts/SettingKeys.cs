namespace Hrms.Settings.Contracts;

public static class SettingKeys
{
    public const string CompanyName = "company.name";
    public const string CompanyTimezone = "company.timezone";
    public const string AttendanceHalfDayHours = "attendance.half_day_hours";
    public const string AttendanceHolidayPriority = "attendance.holiday_priority";
    public const string LeaveYearStart = "leave.year_start";
    public const string PayrollPerDayDivisor = "payroll.per_day_divisor";
    public const string EmployeeCodeFormat = "employee.code_format";

    public static readonly IReadOnlyList<string> All = new[]
    {
        CompanyName,
        CompanyTimezone,
        AttendanceHalfDayHours,
        AttendanceHolidayPriority,
        LeaveYearStart,
        PayrollPerDayDivisor,
        EmployeeCodeFormat
    };
}
