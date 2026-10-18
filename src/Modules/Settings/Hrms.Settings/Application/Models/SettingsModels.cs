namespace Hrms.Settings.Application.Models;

public record SettingDto(
    Guid SettingId,
    string SettingKey,
    string ValueText,
    string? Description,
    DateTime UpdatedAtUtc,
    Guid? UpdatedByUserId);

public record UpdateSettingsCommand(
    string CompanyName,
    string CompanyTimezone,
    string AttendanceHalfDayHours,
    string AttendanceHolidayPriority,
    string LeaveYearStart,
    string PayrollPerDayDivisor,
    string EmployeeCodeFormat);
