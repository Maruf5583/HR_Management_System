using Hrms.Attendance.Application.Commands.SaveDailySheet;
using Hrms.Attendance.Application.Commands.UpdateAttendanceRecord;
using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Application.Queries.GetDailySheet;
using Hrms.Shared.Abstractions;
using Hrms.Shared.Constants;
using Hrms.Web.Areas.Attendance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hrms.Web.Areas.Attendance.Controllers;

[Area("Attendance")]
[Authorize]
public class DailySheetController : Controller
{
    private readonly IGetDailySheetHandler _dailySheetHandler;
    private readonly ISaveDailySheetHandler _saveDailySheetHandler;
    private readonly IUpdateAttendanceRecordHandler _updateAttendanceRecordHandler;
    private readonly IClock _clock;
    private readonly ICurrentUser _currentUser;

    public DailySheetController(
        IGetDailySheetHandler dailySheetHandler,
        ISaveDailySheetHandler saveDailySheetHandler,
        IUpdateAttendanceRecordHandler updateAttendanceRecordHandler,
        IClock clock,
        ICurrentUser currentUser)
    {
        _dailySheetHandler = dailySheetHandler;
        _saveDailySheetHandler = saveDailySheetHandler;
        _updateAttendanceRecordHandler = updateAttendanceRecordHandler;
        _clock = clock;
        _currentUser = currentUser;
    }

    [Authorize(Policy = Permissions.Attendance.ViewAll)]
    [HttpGet]
    public async Task<IActionResult> Index(DateOnly? date)
    {
        var workDate = date ?? _clock.TodayLocal;
        var sheetDto = await _dailySheetHandler.HandleAsync(new GetDailySheetQuery(workDate));

        var viewModel = new DailySheetViewModel
        {
            WorkDate = sheetDto.WorkDate,
            IsHoliday = sheetDto.IsHoliday,
            HolidayName = sheetDto.HolidayName,
            IsWeeklyOff = sheetDto.IsWeeklyOff,
            CanManage = _currentUser.HasPermission(Permissions.Attendance.Manage),
            Items = sheetDto.Items.Select(i => new DailySheetRowViewModel
            {
                EmployeeId = i.EmployeeId,
                EmployeeCode = i.EmployeeCode,
                FullName = i.FullName,
                AttendanceDayId = i.AttendanceDayId,
                CheckInTime = i.CheckInTime,
                CheckOutTime = i.CheckOutTime,
                LateMinutes = i.LateMinutes,
                WorkedMinutes = i.WorkedMinutes,
                Status = i.Status,
                Note = i.Note,
                ShiftName = i.ShiftName,
                ShiftStartTime = i.ShiftStartTime,
                ShiftEndTime = i.ShiftEndTime,
                IsReadonly = i.IsReadonly,
                RowVersionBase64 = i.RowVersion != null ? Convert.ToBase64String(i.RowVersion) : null
            }).ToList()
        };

        return View(viewModel);
    }

    [Authorize(Policy = Permissions.Attendance.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveDailySheetInputModel model)
    {
        if (model.Items == null || model.Items.Count == 0)
        {
            TempData["ErrorMessage"] = "No attendance rows were submitted.";
            return RedirectToAction(nameof(Index), new { date = model.WorkDate.ToString("yyyy-MM-dd") });
        }

        var items = model.Items.Select(x => new DailySheetItemInput(
            x.EmployeeId,
            x.CheckInTime,
            x.CheckOutTime,
            x.Note
        )).ToList();

        var result = await _saveDailySheetHandler.HandleAsync(new SaveDailySheetCommand(model.WorkDate, items));
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = $"Daily attendance sheet for {model.WorkDate:yyyy-MM-dd} saved successfully.";
        }

        return RedirectToAction(nameof(Index), new { date = model.WorkDate.ToString("yyyy-MM-dd") });
    }

    [Authorize(Policy = Permissions.Attendance.Manage)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UpdateAttendanceRecordInputModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Note))
        {
            TempData["ErrorMessage"] = "A note is required when editing an existing attendance record.";
            return RedirectToAction(nameof(Index), new { date = model.WorkDate.ToString("yyyy-MM-dd") });
        }

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(model.RowVersion);
        }
        catch
        {
            TempData["ErrorMessage"] = "Invalid record version data.";
            return RedirectToAction(nameof(Index), new { date = model.WorkDate.ToString("yyyy-MM-dd") });
        }

        var command = new UpdateAttendanceRecordCommand(
            model.AttendanceDayId,
            model.CheckInTime,
            model.CheckOutTime,
            model.Note.Trim(),
            rowVersion
        );

        var result = await _updateAttendanceRecordHandler.HandleAsync(command);
        if (!result.IsSuccess)
        {
            TempData["ErrorMessage"] = result.Error.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Attendance record updated successfully.";
        }

        return RedirectToAction(nameof(Index), new { date = model.WorkDate.ToString("yyyy-MM-dd") });
    }
}
