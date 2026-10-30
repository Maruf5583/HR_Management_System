using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Controllers;

public class HomeController : Controller
{
    private readonly IReportsQueryService _reportsQueryService;

    public HomeController(IReportsQueryService reportsQueryService)
    {
        _reportsQueryService = reportsQueryService;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToAction("Login", "Account");
        }

        DashboardSummaryDto? dashboard = null;
        if (User.IsInRole("Admin") || User.IsInRole("HR"))
        {
            dashboard = await _reportsQueryService.GetDashboardSummaryAsync();
        }

        return View(dashboard);
    }
}
