using Hrms.Attendance.Application.Queries.GetMonthlyAttendance;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Attendance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Attendance.Controllers;

[Area("Attendance")]
[Authorize]
public class MonthlyController : Controller
{
    private readonly IGetMonthlyAttendanceHandler _monthlyAttendanceHandler;
    private readonly IClock _clock;

    public MonthlyController(
        IGetMonthlyAttendanceHandler monthlyAttendanceHandler,
        IClock clock)
    {
        _monthlyAttendanceHandler = monthlyAttendanceHandler;
        _clock = clock;
    }

    [Authorize(Policy = Permissions.Attendance.ViewAll)]
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? search)
    {
        var today = _clock.TodayLocal;
        int y = year ?? today.Year;
        int m = month ?? today.Month;

        if (m < 1 || m > 12)
        {
            m = today.Month;
        }

        var result = await _monthlyAttendanceHandler.HandleAsync(new GetMonthlyAttendanceQuery(y, m, search));

        var viewModel = new MonthlyAttendanceViewModel
        {
            Year = result.Year,
            Month = result.Month,
            DaysInMonth = result.DaysInMonth,
            SearchTerm = search,
            Employees = result.Employees
        };

        return View(viewModel);
    }
}
