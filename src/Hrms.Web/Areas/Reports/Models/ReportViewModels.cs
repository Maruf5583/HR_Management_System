using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Domain.Enums;
using Hrms.Leave.Domain.Enums;
using Hrms.Reports.Application.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Reports.Models;

public class AttendanceReportViewModel
{
    public AttendanceReportFilter Filter { get; set; } = new(null, null, null, null, null);
    public AttendanceReportSummaryDto? Summary { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? Employees { get; set; }
}

public class LeaveReportViewModel
{
    public string ActiveTab { get; set; } = "balances";
    public LeaveBalanceReportFilter BalanceFilter { get; set; } = new(null, null, null);
    public LeaveRecordReportFilter RecordFilter { get; set; } = new(null, null, null, null, null);
    public IReadOnlyList<LeaveBalanceReportItemDto> Balances { get; set; } = new List<LeaveBalanceReportItemDto>();
    public IReadOnlyList<LeaveRecordReportItemDto> Records { get; set; } = new List<LeaveRecordReportItemDto>();
    public SelectList? Departments { get; set; }
    public SelectList? LeaveTypes { get; set; }
}

public class EmployeeReportViewModel
{
    public EmployeeReportFilter Filter { get; set; } = new(null, null, null, null);
    public IReadOnlyList<EmployeeReportItemDto> Employees { get; set; } = new List<EmployeeReportItemDto>();
    public SelectList? Departments { get; set; }
    public SelectList? Designations { get; set; }
}

public class AssetReportViewModel
{
    public AssetReportFilter Filter { get; set; } = new(null, null, null);
    public IReadOnlyList<AssetReportItemDto> Assets { get; set; } = new List<AssetReportItemDto>();
    public SelectList? Departments { get; set; }
}
