using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Employees.Controllers;

[Area("Employees")]
[Authorize]
public class BankDetailsController : Controller
{
    private readonly IBankDetailService _bankDetailService;
    private readonly ICurrentUser _currentUser;

    public BankDetailsController(
        IBankDetailService bankDetailService,
        ICurrentUser currentUser)
    {
        _bankDetailService = bankDetailService;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Permissions.Employees.ViewOwn)]
    public async Task<IActionResult> Submit(SubmitBankDetailModel model)
    {
        if (!_currentUser.EmployeeId.HasValue || _currentUser.EmployeeId.Value == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction("MyProfile", "Employees");
        }

        if (!_currentUser.UserId.HasValue)
        {
            TempData["ErrorMessage"] = "User session expired.";
            return RedirectToAction("MyProfile", "Employees");
        }

        // Always take employee id from ICurrentUser.EmployeeId (Security Rule)
        var submitModel = new SubmitBankDetailModel(
            _currentUser.EmployeeId.Value,
            model.BankName,
            model.AccountHolderName,
            model.AccountNumber,
            model.BranchName,
            model.RoutingNumber,
            model.SwiftCode
        );

        var result = await _bankDetailService.SubmitBankDetailAsync(submitModel, _currentUser.UserId.Value);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
            return RedirectToAction("MyProfile", "Employees");
        }

        TempData["SuccessMessage"] = "Bank details submitted successfully! They will appear as approved on your profile once reviewed by HR or Admin.";
        return RedirectToAction("MyProfile", "Employees");
    }
}
