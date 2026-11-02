using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize(Policy = Permissions.Employees.Manage)]
public class BankApprovalsController : Controller
{
    private readonly IBankDetailService _bankDetailService;
    private readonly ICurrentUser _currentUser;

    public BankApprovalsController(
        IBankDetailService bankDetailService,
        ICurrentUser currentUser)
    {
        _bankDetailService = bankDetailService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pending = await _bankDetailService.GetPendingRequestsAsync();
        return View(pending);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? reviewRemarks, string? returnUrl = null)
    {
        if (!_currentUser.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "Session expired.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _bankDetailService.ReviewRequestAsync(
            new ReviewBankDetailModel(id, true, reviewRemarks),
            _currentUser.UserId.Value
        );

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Bank details approved successfully.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? reviewRemarks, string? returnUrl = null)
    {
        if (!_currentUser.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "Session expired.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _bankDetailService.ReviewRequestAsync(
            new ReviewBankDetailModel(id, false, reviewRemarks),
            _currentUser.UserId.Value
        );

        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Bank details request has been rejected.";
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }
}
