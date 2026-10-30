using Hrms.Employees.Contracts;
using Hrms.Leave.Application.Commands.LeaveRecords;
using Hrms.Leave.Application.Queries.LeaveRecords;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Leave.Domain.Enums;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Leave.Controllers;

[Area("Leave")]
[Authorize]
public class LeaveRecordsController : Controller
{
    private readonly IGetLeaveRecordsHandler _recordsHandler;
    private readonly IRecordDirectLeaveHandler _directRecordHandler;
    private readonly ICancelApprovedLeaveHandler _cancelHandler;
    private readonly IGetLeaveTypesHandler _typesHandler;
    private readonly IEmployeeLookup _employeeLookup;

    public LeaveRecordsController(
        IGetLeaveRecordsHandler recordsHandler,
        IRecordDirectLeaveHandler directRecordHandler,
        ICancelApprovedLeaveHandler cancelHandler,
        IGetLeaveTypesHandler typesHandler,
        IEmployeeLookup employeeLookup)
    {
        _recordsHandler = recordsHandler;
        _directRecordHandler = directRecordHandler;
        _cancelHandler = cancelHandler;
        _typesHandler = typesHandler;
        _employeeLookup = employeeLookup;
    }

    [Authorize(Policy = Permissions.Leave.ViewAll)]
    [HttpGet]
    public async Task<IActionResult> Index(
        Guid? employeeId,
        Guid? leaveTypeId,
        LeaveRecordStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        int page = 1)
    {
        const int pageSize = 15;
        var query = new GetLeaveRecordsQuery(
            EmployeeId: employeeId,
            LeaveTypeId: leaveTypeId,
            Status: status,
            FromDate: fromDate,
            ToDate: toDate,
            SearchTerm: search,
            PageNumber: page,
            PageSize: pageSize
        );

        var result = await _recordsHandler.HandleAsync(query);

        var types = await _typesHandler.HandleAsync(new GetLeaveTypesQuery());
        ViewBag.LeaveTypes = new SelectList(types, "LeaveTypeId", "Name", leaveTypeId);
        ViewBag.Status = status;
        ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
        ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
        ViewBag.Search = search;

        return View(result);
    }

    [Authorize(Policy = Permissions.Leave.ViewAll)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var record = await _recordsHandler.GetDetailsAsync(id);
        if (record == null)
        {
            TempData["ErrorMessage"] = "Leave record not found.";
            return RedirectToAction(nameof(Index));
        }

        return View(record);
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View(new CreateDirectLeaveViewModel
        {
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today)
        });
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDirectLeaveViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync();
            return View(model);
        }

        var command = new RecordDirectLeaveCommand(
            model.EmployeeId,
            model.LeaveTypeId,
            model.StartDate,
            model.EndDate,
            model.IsHalfDay,
            model.Reason
        );

        var result = await _directRecordHandler.HandleAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            await PopulateDropdownsAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = "Direct leave record saved and approved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var result = await _cancelHandler.HandleAsync(new CancelApprovedLeaveCommand(id));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Approved leave record was cancelled and employee balance restored.";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdownsAsync()
    {
        var employees = await _employeeLookup.GetActiveEmployeesDropdownAsync();
        ViewBag.Employees = new SelectList(
            employees.Select(e => new { e.EmployeeId, Display = $"{e.EmployeeCode} - {e.FullName}" }),
            "EmployeeId", "Display");

        var types = await _typesHandler.HandleAsync(new GetLeaveTypesQuery(ActiveOnly: true));
        ViewBag.LeaveTypes = new SelectList(types, "LeaveTypeId", "Name");
    }
}

public class CreateDirectLeaveViewModel
{
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? Reason { get; set; }
}
