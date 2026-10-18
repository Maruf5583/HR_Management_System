using System.ComponentModel.DataAnnotations;

namespace Hrms.Web.Areas.Settings.Models;

public class SettingsViewModel
{
    // Company Group
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyNameDescription { get; set; }

    [Display(Name = "Company Timezone")]
    public string CompanyTimezone { get; set; } = string.Empty;
    public string? CompanyTimezoneDescription { get; set; }

    // Attendance Group
    [Display(Name = "Half Day Hours")]
    public string AttendanceHalfDayHours { get; set; } = string.Empty;
    public string? AttendanceHalfDayHoursDescription { get; set; }

    [Display(Name = "Holiday Priority")]
    public string AttendanceHolidayPriority { get; set; } = "Holiday";
    public string? AttendanceHolidayPriorityDescription { get; set; }

    // Leave Group
    [Display(Name = "Leave Year Start")]
    public string LeaveYearStart { get; set; } = string.Empty;
    public string? LeaveYearStartDescription { get; set; }

    // Payroll Group
    [Display(Name = "Payroll Per-Day Divisor")]
    public string PayrollPerDayDivisor { get; set; } = string.Empty;
    public string? PayrollPerDayDivisorDescription { get; set; }

    // Employee Group
    [Display(Name = "Employee Code Format")]
    public string EmployeeCodeFormat { get; set; } = string.Empty;
    public string? EmployeeCodeFormatDescription { get; set; }
}
