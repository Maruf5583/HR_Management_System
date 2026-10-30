using Hrms.Leave.Application.Commands.LeaveTypes;
using Hrms.Leave.Application.Queries.LeaveTypes;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Leave.Controllers;

[Area("Leave")]
[Authorize]
public class LeaveTypesController : Controller
{
    private readonly IGetLeaveTypesHandler _getHandler;
    private readonly ICreateLeaveTypeHandler _createHandler;
    private readonly IUpdateLeaveTypeHandler _updateHandler;
    private readonly IToggleLeaveTypeStatusHandler _toggleStatusHandler;

    public LeaveTypesController(
        IGetLeaveTypesHandler getHandler,
        ICreateLeaveTypeHandler createHandler,
        IUpdateLeaveTypeHandler updateHandler,
        IToggleLeaveTypeStatusHandler toggleStatusHandler)
    {
        _getHandler = getHandler;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _toggleStatusHandler = toggleStatusHandler;
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var types = await _getHandler.HandleAsync(new GetLeaveTypesQuery(ActiveOnly: false));
        return View(types);
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateLeaveTypeViewModel());
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateLeaveTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateLeaveTypeCommand(
            model.Name,
            model.Code,
            model.IsPaid,
            model.DaysPerYear,
            model.RequiresDocument
        );

        var result = await _createHandler.HandleAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = $"Leave type '{model.Name}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var type = await _getHandler.GetByIdAsync(id);
        if (type == null)
        {
            TempData["ErrorMessage"] = "Leave type not found.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EditLeaveTypeViewModel
        {
            LeaveTypeId = type.LeaveTypeId,
            Name = type.Name,
            Code = type.Code,
            IsPaid = type.IsPaid,
            DaysPerYear = type.DaysPerYear,
            RequiresDocument = type.RequiresDocument
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditLeaveTypeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new UpdateLeaveTypeCommand(
            model.LeaveTypeId,
            model.Name,
            model.Code,
            model.IsPaid,
            model.DaysPerYear,
            model.RequiresDocument
        );

        var result = await _updateHandler.HandleAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return View(model);
        }

        TempData["SuccessMessage"] = $"Leave type '{model.Name}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = Permissions.Leave.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var result = await _toggleStatusHandler.HandleAsync(new ToggleLeaveTypeStatusCommand(id));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Leave type status toggled successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}

public class CreateLeaveTypeViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = true;
    public decimal DaysPerYear { get; set; } = 15m;
    public bool RequiresDocument { get; set; }
}

public class EditLeaveTypeViewModel
{
    public Guid LeaveTypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal DaysPerYear { get; set; }
    public bool RequiresDocument { get; set; }
}
