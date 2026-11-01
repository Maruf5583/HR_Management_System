using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Models;
using Hrms.Shared.Abstractions;
using Hrms.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IReportsQueryService _reportsQueryService;
    private readonly ICurrentUser _currentUser;

    public HomeController(
        IReportsQueryService reportsQueryService,
        ICurrentUser currentUser)
    {
        _reportsQueryService = reportsQueryService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? view = null, int? year = null, int? month = null)
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account");
        }

        var vm = new HomeDashboardViewModel();

        var isManagementRole = User.IsInRole("Admin") || User.IsInRole("HR");
        var hasEmployeeProfile = _currentUser.EmployeeId.HasValue;

        if (isManagementRole)
        {
            vm.ManagementDashboard = await _reportsQueryService.GetDashboardSummaryAsync();
        }

        if (hasEmployeeProfile)
        {
            vm.EmployeeDashboard = await _reportsQueryService.GetEmployeeDashboardAsync(_currentUser.EmployeeId!.Value, year, month);
        }

        if (isManagementRole)
        {
            vm.IsManagementDashboard = view != "employee";
        }
        else
        {
            vm.IsManagementDashboard = false;
        }

        return View(vm);
    }
}
