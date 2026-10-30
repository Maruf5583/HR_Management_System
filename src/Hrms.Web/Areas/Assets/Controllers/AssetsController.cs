using System.ComponentModel.DataAnnotations;
using Hrms.Assets.Application.Models;
using Hrms.Assets.Application.Services;
using Hrms.Assets.Domain.Enums;
using Hrms.Employees.Contracts;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Hrms.Web.Areas.Assets.Controllers;

[Area("Assets")]
[Authorize]
public class AssetsController : Controller
{
    private readonly IAssetService _assetService;
    private readonly IEmployeeLookup _employeeLookup;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public AssetsController(
        IAssetService assetService,
        IEmployeeLookup employeeLookup,
        ICurrentUser currentUser,
        IClock clock)
    {
        _assetService = assetService;
        _employeeLookup = employeeLookup;
        _currentUser = currentUser;
        _clock = clock;
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        AssetCategory? category,
        AssetStatus? status,
        int page = 1)
    {
        const int pageSize = 15;
        var pagedResult = await _assetService.GetAssetsAsync(search, category, status, page, pageSize);

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Status = status;

        return View(pagedResult);
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateAssetViewModel
        {
            PurchaseDate = DateOnly.FromDateTime(_clock.UtcNow)
        });
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAssetViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new CreateAssetCommand(
            model.AssetTag,
            model.Name,
            model.Category,
            model.SerialNumber,
            model.PurchaseDate,
            model.Condition,
            model.Notes);

        var result = await _assetService.CreateAssetAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Asset '{model.AssetTag}' created successfully.";
        return RedirectToAction(nameof(Details), new { id = result.Value });
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var asset = await _assetService.GetAssetDetailsAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        var history = await _assetService.GetAssetHistoryAsync(id);
        ViewBag.History = history;

        return View(asset);
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var asset = await _assetService.GetAssetDetailsAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        var model = new EditAssetViewModel
        {
            AssetId = asset.AssetId,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Category = asset.Category,
            SerialNumber = asset.SerialNumber,
            PurchaseDate = asset.PurchaseDate,
            Condition = asset.Condition,
            Status = asset.Status,
            Notes = asset.Notes,
            RowVersion = Convert.ToBase64String(asset.RowVersion)
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditAssetViewModel model)
    {
        if (id != model.AssetId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var rowVersion = Convert.FromBase64String(model.RowVersion);
        var command = new UpdateAssetCommand(
            model.AssetId,
            model.Name,
            model.Category,
            model.SerialNumber,
            model.PurchaseDate,
            model.Condition,
            model.Status,
            model.Notes,
            rowVersion);

        var result = await _assetService.UpdateAssetAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Asset '{model.AssetTag}' updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.AssetId });
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public async Task<IActionResult> Assign(Guid id)
    {
        var asset = await _assetService.GetAssetDetailsAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        if (asset.Status != AssetStatus.Available)
        {
            TempData["ErrorMessage"] = $"Asset '{asset.AssetTag}' is {asset.Status} and cannot be assigned.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await PopulateEmployeesDropdownAsync();

        var model = new AssignAssetViewModel
        {
            AssetId = asset.AssetId,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            AssignedAtUtc = _clock.UtcNow
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid id, AssignAssetViewModel model)
    {
        if (id != model.AssetId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateEmployeesDropdownAsync();
            return View(model);
        }

        var command = new AssignAssetCommand(
            model.AssetId,
            model.EmployeeId,
            model.AssignedAtUtc,
            model.Note);

        var result = await _assetService.AssignAssetAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            await PopulateEmployeesDropdownAsync();
            return View(model);
        }

        TempData["SuccessMessage"] = $"Asset '{model.AssetTag}' has been assigned successfully.";
        return RedirectToAction(nameof(Details), new { id = model.AssetId });
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpGet]
    public async Task<IActionResult> Return(Guid id)
    {
        var asset = await _assetService.GetAssetDetailsAsync(id);
        if (asset == null)
        {
            return NotFound();
        }

        if (asset.Status != AssetStatus.Assigned || !asset.CurrentAssignmentId.HasValue)
        {
            TempData["ErrorMessage"] = $"Asset '{asset.AssetTag}' is not currently assigned.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var model = new ReturnAssetViewModel
        {
            AssetId = asset.AssetId,
            AssetTag = asset.AssetTag,
            AssetName = asset.Name,
            AssignedEmployeeName = asset.CurrentEmployeeName ?? "Employee",
            AssignedAtUtc = asset.CurrentAssignedAtUtc ?? _clock.UtcNow,
            ReturnedAtUtc = _clock.UtcNow,
            ReturnedCondition = asset.Condition,
            TargetStatus = AssetStatus.Available
        };

        return View(model);
    }

    [Authorize(Policy = Permissions.Assets.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(Guid id, ReturnAssetViewModel model)
    {
        if (id != model.AssetId)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var command = new ReturnAssetCommand(
            model.AssetId,
            model.ReturnedAtUtc,
            model.ReturnedCondition,
            model.TargetStatus,
            model.Note);

        var result = await _assetService.ReturnAssetAsync(command);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = $"Asset '{model.AssetTag}' marked as returned successfully.";
        return RedirectToAction(nameof(Details), new { id = model.AssetId });
    }

    [Authorize(Policy = Permissions.Assets.ViewOwn)]
    [HttpGet]
    public async Task<IActionResult> My()
    {
        // Never trust ID from URL; always take employee id from ICurrentUser.EmployeeId
        var employeeId = _currentUser.EmployeeId;
        if (!employeeId.HasValue)
        {
            return View("NoLinkedEmployee");
        }

        var myAssets = await _assetService.GetMyAssetsAsync(employeeId.Value);
        return View(myAssets);
    }

    private async Task PopulateEmployeesDropdownAsync()
    {
        var employees = await _employeeLookup.GetActiveEmployeesDropdownAsync();
        ViewBag.Employees = employees.Select(e => new SelectListItem
        {
            Value = e.EmployeeId.ToString(),
            Text = $"{e.FullName} ({e.EmployeeCode})"
        }).ToList();
    }
}

public class CreateAssetViewModel
{
    [Required(ErrorMessage = "Asset tag is required")]
    [StringLength(50)]
    [Display(Name = "Asset Tag (e.g., AST-001)")]
    public string AssetTag { get; set; } = string.Empty;

    [Required(ErrorMessage = "Asset name is required")]
    [StringLength(150)]
    [Display(Name = "Asset Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    public AssetCategory Category { get; set; } = AssetCategory.Laptop;

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Purchase Date")]
    public DateOnly? PurchaseDate { get; set; }

    [Required]
    [Display(Name = "Condition")]
    public AssetCondition Condition { get; set; } = AssetCondition.New;

    [StringLength(1000)]
    [Display(Name = "Notes / Specifications")]
    public string? Notes { get; set; }
}

public class EditAssetViewModel
{
    public Guid AssetId { get; set; }

    public string AssetTag { get; set; } = string.Empty;

    [Required(ErrorMessage = "Asset name is required")]
    [StringLength(150)]
    [Display(Name = "Asset Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Category")]
    public AssetCategory Category { get; set; }

    [StringLength(100)]
    [Display(Name = "Serial Number")]
    public string? SerialNumber { get; set; }

    [Display(Name = "Purchase Date")]
    public DateOnly? PurchaseDate { get; set; }

    [Required]
    [Display(Name = "Condition")]
    public AssetCondition Condition { get; set; }

    [Required]
    [Display(Name = "Status")]
    public AssetStatus Status { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes / Specifications")]
    public string? Notes { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}

public class AssignAssetViewModel
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an employee")]
    [Display(Name = "Assign To Employee")]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Assignment date is required")]
    [Display(Name = "Assignment Date & Time (UTC)")]
    public DateTime AssignedAtUtc { get; set; }

    [StringLength(500)]
    [Display(Name = "Assignment Note")]
    public string? Note { get; set; }
}

public class ReturnAssetViewModel
{
    public Guid AssetId { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string AssetName { get; set; } = string.Empty;
    public string AssignedEmployeeName { get; set; } = string.Empty;
    public DateTime AssignedAtUtc { get; set; }

    [Required(ErrorMessage = "Return date is required")]
    [Display(Name = "Return Date & Time (UTC)")]
    public DateTime ReturnedAtUtc { get; set; }

    [Required]
    [Display(Name = "Condition Upon Return")]
    public AssetCondition ReturnedCondition { get; set; } = AssetCondition.Good;

    [Required]
    [Display(Name = "Asset Status Post-Return")]
    public AssetStatus TargetStatus { get; set; } = AssetStatus.Available;

    [StringLength(500)]
    [Display(Name = "Return Note / Check Remarks")]
    public string? Note { get; set; }
}
