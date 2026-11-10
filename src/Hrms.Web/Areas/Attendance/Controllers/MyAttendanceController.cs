using Hrms.Attendance.Application.Commands.SelfService;
using Hrms.Attendance.Application.Queries.GetAttendanceRequests;
using Hrms.Attendance.Application.Queries.GetEmployeeAttendance;
using Hrms.Attendance.Application.Queries.GetMyAttendanceSession;
using Hrms.Attendance.Domain.Enums;
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
    private readonly IGetMyAttendanceSessionHandler _myAttendanceSessionHandler;
    private readonly IGetAttendanceRequestsHandler _attendanceRequestsHandler;
    private readonly IClockInHandler _clockInHandler;
    private readonly IStartBreakHandler _startBreakHandler;
    private readonly IEndBreakHandler _endBreakHandler;
    private readonly IClockOutHandler _clockOutHandler;
    private readonly ISubmitAttendanceCorrectionHandler _submitCorrectionHandler;
    private readonly ICancelAttendanceCorrectionHandler _cancelCorrectionHandler;
    private readonly ICurrentUser _currentUser;
    private readonly IClock _clock;

    public MyAttendanceController(
        IGetEmployeeAttendanceHandler employeeAttendanceHandler,
        IGetMyAttendanceSessionHandler myAttendanceSessionHandler,
        IGetAttendanceRequestsHandler attendanceRequestsHandler,
        IClockInHandler clockInHandler,
        IStartBreakHandler startBreakHandler,
        IEndBreakHandler endBreakHandler,
        IClockOutHandler clockOutHandler,
        ISubmitAttendanceCorrectionHandler submitCorrectionHandler,
        ICancelAttendanceCorrectionHandler cancelCorrectionHandler,
        ICurrentUser currentUser,
        IClock clock)
    {
        _employeeAttendanceHandler = employeeAttendanceHandler;
        _myAttendanceSessionHandler = myAttendanceSessionHandler;
        _attendanceRequestsHandler = attendanceRequestsHandler;
        _clockInHandler = clockInHandler;
        _startBreakHandler = startBreakHandler;
        _endBreakHandler = endBreakHandler;
        _clockOutHandler = clockOutHandler;
        _submitCorrectionHandler = submitCorrectionHandler;
        _cancelCorrectionHandler = cancelCorrectionHandler;
        _currentUser = currentUser;
        _clock = clock;
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpGet]
    public async Task<IActionResult> Index(int? year, int? month, string? tab = "attendance")
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            var todayLocal = _clock.TodayLocal;
            return View(new MyAttendanceViewModel
            {
                HasEmployee = false,
                Year = todayLocal.Year,
                Month = todayLocal.Month,
                ActiveTab = string.IsNullOrWhiteSpace(tab) ? "attendance" : tab.ToLowerInvariant()
            });
        }

        var empId = _currentUser.EmployeeId.Value;
        var today = _clock.TodayLocal;
        int y = year ?? today.Year;
        int m = month ?? today.Month;

        if (m < 1 || m > 12)
        {
            m = today.Month;
        }

        var report = await _employeeAttendanceHandler.HandleAsync(
            new GetEmployeeAttendanceQuery(empId, y, m));

        var session = await _myAttendanceSessionHandler.HandleAsync(
            new GetMyAttendanceSessionQuery(empId));

        var requests = await _attendanceRequestsHandler.GetEmployeeRequestsAsync(
            new GetEmployeeAttendanceRequestsQuery(empId));

        double totalLoggedHours = 0;
        double totalOvertimeHours = 0;
        int workingDays = 0;

        int standardShiftMins = 480;
        if (session?.ShiftStart.HasValue == true && session?.ShiftEnd.HasValue == true)
        {
            var diff = session.ShiftEnd.Value - session.ShiftStart.Value;
            if (diff < TimeSpan.Zero) diff += TimeSpan.FromHours(24);
            if (diff.TotalMinutes > 0)
            {
                standardShiftMins = (int)diff.TotalMinutes;
            }
        }

        if (report?.Days != null)
        {
            totalLoggedHours = Math.Round(report.Days.Sum(d => d.WorkedMinutes) / 60.0, 1);
            // Overtime is any excess beyond standard shift duration per working day
            totalOvertimeHours = Math.Round(report.Days.Sum(d => Math.Max(0, d.WorkedMinutes - standardShiftMins)) / 60.0, 1);
            workingDays = report.Days.Count(d => d.Status != AttendanceStatus.Holiday && d.Status != AttendanceStatus.Weekend);
        }

        int scorePercent = 100;
        if (report != null && (report.PresentDays + report.LateDays) > 0)
        {
            scorePercent = (int)Math.Round((double)report.PresentDays / (report.PresentDays + report.LateDays) * 100);
        }

        var viewModel = new MyAttendanceViewModel
        {
            HasEmployee = true,
            Year = y,
            Month = m,
            ActiveTab = string.IsNullOrWhiteSpace(tab) ? "attendance" : tab.ToLowerInvariant(),
            Session = session,
            Report = report,
            MyRequests = requests,
            TotalLoggedHours = totalLoggedHours,
            TotalOvertimeHours = totalOvertimeHours,
            WorkingDaysCount = workingDays,
            ScorePercent = scorePercent
        };

        return View(viewModel);
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn()
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _clockInHandler.HandleAsync(new ClockInCommand(_currentUser.EmployeeId.Value));
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Work session started successfully!";
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartBreak()
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _startBreakHandler.HandleAsync(new StartBreakCommand(_currentUser.EmployeeId.Value));
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Break started.";
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndBreak()
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _endBreakHandler.HandleAsync(new EndBreakCommand(_currentUser.EmployeeId.Value));
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Break ended. Welcome back to work!";
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut()
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _clockOutHandler.HandleAsync(new ClockOutCommand(_currentUser.EmployeeId.Value));
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Clocked out successfully for today!";
        }

        return RedirectToAction(nameof(Index), new { tab = "attendance" });
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitCorrection(SubmitCorrectionInputModel model)
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index), new { tab = "fix-attendance" });
        }

        var command = new SubmitAttendanceCorrectionCommand(
            _currentUser.EmployeeId.Value,
            model.WorkDate,
            model.CheckInTime,
            model.CheckOutTime,
            model.Reason
        );

        var result = await _submitCorrectionHandler.HandleAsync(command);
        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Attendance regularization request submitted successfully. Waiting for HR/Admin approval.";
        }

        return RedirectToAction(nameof(Index), new { tab = "fix-attendance" });
    }

    [Authorize(Policy = Permissions.Attendance.ViewOwn)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelCorrection(Guid requestId)
    {
        if (_currentUser.EmployeeId == null || _currentUser.EmployeeId == Guid.Empty)
        {
            TempData["ErrorMessage"] = "No linked employee profile found.";
            return RedirectToAction(nameof(Index), new { tab = "fix-attendance" });
        }

        var result = await _cancelCorrectionHandler.HandleAsync(
            new CancelAttendanceCorrectionCommand(requestId, _currentUser.EmployeeId.Value));

        if (result.IsFailure)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Attendance regularization request was cancelled successfully.";
        }

        return RedirectToAction(nameof(Index), new { tab = "fix-attendance" });
    }
}
