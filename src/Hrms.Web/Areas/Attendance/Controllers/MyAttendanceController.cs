using Hrms.Attendance.Application.Queries.GetEmployeeAttendance;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Attendance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Attendance.Controllers;

[Area("Attendance")]
[Authorize]
public class MyAttendanceController : Controller
{
    private readonly IGetEmployeeAttendanceHandler _employeeAttendanceHandler;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MyAttendanceController(
        IGetEmployeeAttendanceHandler employeeAttendanceHandler,
        ICurrentUser currentUser,
        IClock clock)
    {
        _employeeAttendanceHandler = employeeAttendanceHandler;
        _currentUser = currentUser;
        _clock = clock;
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month)
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            return View(new MyAttendanceViewModel
            {
                HasEmployee = false
            });
        }

        var today = _clock.TodayLocal;
        int y = year ?? today.Year;
        int m = month ?? today.Month;

        if (m < 1 || m > 12)
        {
            m = today.Month;
        }

        var report = await _employeeAttendanceHandler.HandleAsync(
            new GetEmployeeAttendanceQuery(_currentUser.EmployeeId.Value, y, m));

        var viewModel = new MyAttendanceViewModel
        {
            HasEmployee = true,
            Year = y,
            Month = m,
            Report = report
        };

        return View(viewModel);
    }
}
