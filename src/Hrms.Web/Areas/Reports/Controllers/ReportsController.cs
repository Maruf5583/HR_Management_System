using Hrms.Assets.Domain.Enums;
using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Domain.Enums;
using Hrms.Leave.Domain.Enums;
using Hrms.Persistence;
using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Models;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Reports.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Web.Areas.Reports.Controllers;

[Area("Reports")]
[Authorize(Policy = Permissions.Reports.View)]
public class ReportsController : Controller
{
    private readonly IReportsQueryService _queryService;
    private readonly IExcelExportService _exportService;
    private readonly HrmsDbContext _context;
    private readonly IClock _clock;

    public ReportsController(
        IReportsQueryService queryService,
        IExcelExportService exportService,
        HrmsDbContext context,
        IClock clock)
    {
        _queryService = queryService;
        _exportService = exportService;
        _context = context;
        _clock = clock;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Attendance(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? departmentId,
        Guid? employeeId,
        AttendanceStatus? status)
    {
        var filter = new AttendanceReportFilter(fromDate, toDate, departmentId, employeeId, status);
        var summary = await _queryService.GetAttendanceReportAsync(filter);

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();

        var employees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active)
            .OrderBy(e => e.FirstName)
            .Select(e => new { e.EmployeeId, FullName = e.EmployeeCode + " - " + e.FirstName + " " + e.LastName })
            .ToListAsync();

        var viewModel = new AttendanceReportViewModel
        {
            Filter = filter,
            Summary = summary,
            Departments = new SelectList(departments, "DepartmentId", "Name", departmentId),
            Employees = new SelectList(employees, "EmployeeId", "FullName", employeeId)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAttendance(
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? departmentId,
        Guid? employeeId,
        AttendanceStatus? status)
    {
        var filter = new AttendanceReportFilter(fromDate, toDate, departmentId, employeeId, status);
        var summary = await _queryService.GetAttendanceReportAsync(filter);
        var fileBytes = _exportService.ExportAttendanceReport(summary);

        var fileName = $"AttendanceReport_{summary.FromDate:yyyyMMdd}_{summary.ToDate:yyyyMMdd}.xlsx";
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> Leave(
        int? year,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? departmentId,
        Guid? leaveTypeId,
        LeaveRecordStatus? status,
        string tab = "balances")
    {
        var currentYear = year ?? _clock.TodayLocal.Year;
        var balanceFilter = new LeaveBalanceReportFilter(currentYear, departmentId, leaveTypeId);
        var recordFilter = new LeaveRecordReportFilter(fromDate, toDate, departmentId, leaveTypeId, status);

        var balances = tab == "balances"
            ? await _queryService.GetLeaveBalancesReportAsync(balanceFilter)
            : new List<LeaveBalanceReportItemDto>();

        var records = tab == "records"
            ? await _queryService.GetLeaveRecordsReportAsync(recordFilter)
            : new List<LeaveRecordReportItemDto>();

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();

        var leaveTypes = await _context.LeaveTypes
            .AsNoTracking()
            .OrderBy(lt => lt.Name)
            .Select(lt => new { lt.LeaveTypeId, lt.Name })
            .ToListAsync();

        var viewModel = new LeaveReportViewModel
        {
            ActiveTab = tab,
            BalanceFilter = balanceFilter,
            RecordFilter = recordFilter,
            Balances = balances,
            Records = records,
            Departments = new SelectList(departments, "DepartmentId", "Name", departmentId),
            LeaveTypes = new SelectList(leaveTypes, "LeaveTypeId", "Name", leaveTypeId)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeaveBalances(int? year, Guid? departmentId, Guid? leaveTypeId)
    {
        var currentYear = year ?? _clock.TodayLocal.Year;
        var filter = new LeaveBalanceReportFilter(currentYear, departmentId, leaveTypeId);
        var list = await _queryService.GetLeaveBalancesReportAsync(filter);
        var fileBytes = _exportService.ExportLeaveBalancesReport(list, currentYear);

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"LeaveBalances_{currentYear}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> ExportLeaveRecords(DateOnly? fromDate, DateOnly? toDate, Guid? departmentId, Guid? leaveTypeId, LeaveRecordStatus? status)
    {
        var filter = new LeaveRecordReportFilter(fromDate, toDate, departmentId, leaveTypeId, status);
        var list = await _queryService.GetLeaveRecordsReportAsync(filter);
        var fileBytes = _exportService.ExportLeaveRecordsReport(list);

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"LeaveRecords_{_clock.TodayLocal:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Employees(
        Guid? departmentId,
        Guid? designationId,
        EmploymentType? employmentType,
        EmployeeStatus? status)
    {
        var filter = new EmployeeReportFilter(departmentId, designationId, employmentType, status);
        var list = await _queryService.GetEmployeeReportAsync(filter);

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();

        var designations = await _context.Designations
            .AsNoTracking()
            .OrderBy(d => d.Title)
            .Select(d => new { d.DesignationId, d.Title })
            .ToListAsync();

        var viewModel = new EmployeeReportViewModel
        {
            Filter = filter,
            Employees = list,
            Departments = new SelectList(departments, "DepartmentId", "Name", departmentId),
            Designations = new SelectList(designations, "DesignationId", "Title", designationId)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportEmployees(
        Guid? departmentId,
        Guid? designationId,
        EmploymentType? employmentType,
        EmployeeStatus? status)
    {
        var filter = new EmployeeReportFilter(departmentId, designationId, employmentType, status);
        var list = await _queryService.GetEmployeeReportAsync(filter);
        var fileBytes = _exportService.ExportEmployeeReport(list);

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"EmployeeDirectory_{_clock.TodayLocal:yyyyMMdd}.xlsx");
    }

    [HttpGet]
    public async Task<IActionResult> Assets(
        AssetCategory? category,
        AssetStatus? status,
        Guid? departmentId)
    {
        var filter = new AssetReportFilter(category, status, departmentId);
        var list = await _queryService.GetAssetReportAsync(filter);

        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new { d.DepartmentId, d.Name })
            .ToListAsync();

        var viewModel = new AssetReportViewModel
        {
            Filter = filter,
            Assets = list,
            Departments = new SelectList(departments, "DepartmentId", "Name", departmentId)
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportAssets(
        AssetCategory? category,
        AssetStatus? status,
        Guid? departmentId)
    {
        var filter = new AssetReportFilter(category, status, departmentId);
        var list = await _queryService.GetAssetReportAsync(filter);
        var fileBytes = _exportService.ExportAssetReport(list);

        return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AssetsReport_{_clock.TodayLocal:yyyyMMdd}.xlsx");
    }
}
