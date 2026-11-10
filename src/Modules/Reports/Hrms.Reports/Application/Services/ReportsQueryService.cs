using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Domain.Enums;
using Hrms.Leave.Domain.Enums;
using Hrms.Persistence;
using Hrms.Reports.Application.Abstractions;
using Hrms.Reports.Application.Models;
using Hrms.Settings.Contracts;
using Hrms.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Reports.Application.Services;

public class ReportsQueryService : IReportsQueryService
{
    private readonly HrmsDbContext _context;
    private readonly IClock _clock;
    private readonly ISettingsReader _settingsReader;

    public ReportsQueryService(
        HrmsDbContext context,
        IClock clock,
        ISettingsReader settingsReader)
    {
        _context = context;
        _clock = clock;
        _settingsReader = settingsReader;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;

        // 1. Master Counts
        var totalActiveEmployees = await _context.Employees
            .AsNoTracking()
            .CountAsync(e => e.Status == EmployeeStatus.Active, cancellationToken);

        var totalDepartments = await _context.Departments
            .AsNoTracking()
            .CountAsync(d => d.Status == EntityStatus.Active, cancellationToken);

        var totalAssignedAssets = await _context.Assets
            .AsNoTracking()
            .CountAsync(a => a.Status == Assets.Domain.Enums.AssetStatus.Assigned, cancellationToken);

        var totalAvailableAssets = await _context.Assets
            .AsNoTracking()
            .CountAsync(a => a.Status == Assets.Domain.Enums.AssetStatus.Available, cancellationToken);

        var pendingLeaveApprovals = await _context.LeaveRecords
            .AsNoTracking()
            .CountAsync(l => l.Status == LeaveRecordStatus.Pending, cancellationToken);

        // 2. Today's Calendar Status
        var todayHoliday = await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HolidayDate == today, cancellationToken);

        var dayOfWeekInt = (int)today.DayOfWeek;
        var workingDayRow = await _context.CompanyWorkingDays
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.DayOfWeek == dayOfWeekInt, cancellationToken);

        var isTodayHoliday = todayHoliday != null;
        var isTodayWeekend = workingDayRow != null && !workingDayRow.IsWorkingDay;

        // 3. Today's Attendance Counts
        var todayAttendanceGroup = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.WorkDate == today)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int present = todayAttendanceGroup.FirstOrDefault(g => g.Status == AttendanceStatus.Present)?.Count ?? 0;
        int late = todayAttendanceGroup.FirstOrDefault(g => g.Status == AttendanceStatus.Late)?.Count ?? 0;
        int halfDay = todayAttendanceGroup.FirstOrDefault(g => g.Status == AttendanceStatus.HalfDay)?.Count ?? 0;
        int absent = todayAttendanceGroup.FirstOrDefault(g => g.Status == AttendanceStatus.Absent)?.Count ?? 0;
        int onLeave = todayAttendanceGroup.FirstOrDefault(g => g.Status == AttendanceStatus.OnLeave)?.Count ?? 0;
        int weekendOrHoliday = todayAttendanceGroup.Where(g => g.Status == AttendanceStatus.Holiday || g.Status == AttendanceStatus.Weekend).Sum(g => g.Count);

        // 4. Upcoming Holidays (Next 30 days)
        var next30Days = today.AddDays(30);
        var holidays = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.HolidayDate >= today && h.HolidayDate <= next30Days)
            .OrderBy(h => h.HolidayDate)
            .Select(h => new UpcomingHolidayDto(
                h.HolidayDate,
                h.Name,
                h.Type.ToString(),
                h.HolidayDate.DayNumber - today.DayNumber
            ))
            .ToListAsync(cancellationToken);

        // 5. Recent Attendance Logs for Today
        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var recentRaw = await (from a in _context.AttendanceDays.AsNoTracking()
                               join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.EmployeeId
                               join d in _context.Departments.AsNoTracking() on e.DepartmentId equals d.DepartmentId
                               where a.WorkDate == today
                               orderby a.CheckInUtc descending
                               select new
                               {
                                   e.EmployeeCode,
                                   FullName = e.FirstName + " " + e.LastName,
                                   Department = d.Name,
                                   a.CheckInUtc,
                                   a.CheckOutUtc,
                                   a.Status,
                                   a.LateMinutes
                               })
                               .Take(8)
                               .ToListAsync(cancellationToken);

        var recentAttendances = recentRaw.Select(x => new RecentAttendanceItemDto(
            x.EmployeeCode,
            x.FullName,
            x.Department,
            x.CheckInUtc.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.CheckInUtc.Value, timeZone)) : null,
            x.CheckOutUtc.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.CheckOutUtc.Value, timeZone)) : null,
            x.Status,
            x.LateMinutes
        )).ToList();

        return new DashboardSummaryDto(
            TotalActiveEmployees: totalActiveEmployees,
            TotalDepartments: totalDepartments,
            TotalAssignedAssets: totalAssignedAssets,
            TotalAvailableAssets: totalAvailableAssets,
            PendingLeaveApprovals: pendingLeaveApprovals,
            TodayDate: today,
            TodayIsHoliday: isTodayHoliday,
            TodayHolidayName: todayHoliday?.Name,
            TodayIsWeekend: isTodayWeekend,
            TodayPresent: present,
            TodayLate: late,
            TodayHalfDay: halfDay,
            TodayAbsent: absent,
            TodayOnLeave: onLeave,
            TodayWeekendOrHoliday: weekendOrHoliday,
            UpcomingHolidays: holidays,
            RecentAttendances: recentAttendances
        );
    }

    public async Task<AttendanceReportSummaryDto> GetAttendanceReportAsync(AttendanceReportFilter filter, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var fromDate = filter.FromDate ?? new DateOnly(today.Year, today.Month, 1);
        var toDate = filter.ToDate ?? today;

        var query = from a in _context.AttendanceDays.AsNoTracking()
                    join e in _context.Employees.AsNoTracking() on a.EmployeeId equals e.EmployeeId
                    join dept in _context.Departments.AsNoTracking() on e.DepartmentId equals dept.DepartmentId
                    join desig in _context.Designations.AsNoTracking() on e.DesignationId equals desig.DesignationId
                    join sv in _context.ShiftVersions.AsNoTracking() on a.ShiftVersionId equals sv.ShiftVersionId into svJoin
                    from shiftVer in svJoin.DefaultIfEmpty()
                    join s in _context.Shifts.AsNoTracking() on shiftVer.ShiftId equals s.ShiftId into sJoin
                    from shift in sJoin.DefaultIfEmpty()
                    where a.WorkDate >= fromDate && a.WorkDate <= toDate
                    select new
                    {
                        a.WorkDate,
                        e.EmployeeId,
                        e.EmployeeCode,
                        FullName = e.FirstName + " " + e.LastName,
                        e.DepartmentId,
                        DepartmentName = dept.Name,
                        DesignationTitle = desig.Title,
                        ShiftName = shift != null ? shift.Name : null,
                        a.CheckInUtc,
                        a.CheckOutUtc,
                        a.LateMinutes,
                        a.WorkedMinutes,
                        a.Status,
                        a.Note
                    };

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.EmployeeId.HasValue)
        {
            query = query.Where(x => x.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        var rawList = await query
            .OrderByDescending(x => x.WorkDate)
            .ThenBy(x => x.EmployeeCode)
            .ToListAsync(cancellationToken);

        var tzId = await _settingsReader.GetStringAsync(SettingKeys.CompanyTimezone, cancellationToken) ?? "Asia/Dhaka";
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        var items = rawList.Select(x => new AttendanceReportItemDto(
            WorkDate: x.WorkDate,
            EmployeeCode: x.EmployeeCode,
            FullName: x.FullName,
            DepartmentName: x.DepartmentName,
            DesignationTitle: x.DesignationTitle,
            ShiftName: x.ShiftName,
            CheckInTime: x.CheckInUtc.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.CheckInUtc.Value, timeZone)) : null,
            CheckOutTime: x.CheckOutUtc.HasValue ? TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(x.CheckOutUtc.Value, timeZone)) : null,
            LateMinutes: x.LateMinutes,
            WorkedMinutes: x.WorkedMinutes,
            Status: x.Status,
            Note: x.Note
        )).ToList();

        return new AttendanceReportSummaryDto(
            FromDate: fromDate,
            ToDate: toDate,
            TotalRecords: items.Count,
            PresentCount: items.Count(i => i.Status == AttendanceStatus.Present),
            LateCount: items.Count(i => i.Status == AttendanceStatus.Late),
            HalfDayCount: items.Count(i => i.Status == AttendanceStatus.HalfDay),
            AbsentCount: items.Count(i => i.Status == AttendanceStatus.Absent),
            OnLeaveCount: items.Count(i => i.Status == AttendanceStatus.OnLeave),
            HolidayCount: items.Count(i => i.Status == AttendanceStatus.Holiday),
            WeekendCount: items.Count(i => i.Status == AttendanceStatus.Weekend),
            Items: items
        );
    }

    public async Task<IReadOnlyList<LeaveBalanceReportItemDto>> GetLeaveBalancesReportAsync(LeaveBalanceReportFilter filter, CancellationToken cancellationToken = default)
    {
        var year = filter.Year ?? _clock.TodayLocal.Year;

        var query = from b in _context.LeaveBalances.AsNoTracking()
                    join e in _context.Employees.AsNoTracking() on b.EmployeeId equals e.EmployeeId
                    join dept in _context.Departments.AsNoTracking() on e.DepartmentId equals dept.DepartmentId
                    join lt in _context.LeaveTypes.AsNoTracking() on b.LeaveTypeId equals lt.LeaveTypeId
                    where b.Year == year
                    select new
                    {
                        e.EmployeeId,
                        e.EmployeeCode,
                        FullName = e.FirstName + " " + e.LastName,
                        e.DepartmentId,
                        DepartmentName = dept.Name,
                        b.LeaveTypeId,
                        LeaveTypeName = lt.Name,
                        b.Year,
                        b.Entitled,
                        b.Used,
                        b.Remaining
                    };

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.LeaveTypeId.HasValue)
        {
            query = query.Where(x => x.LeaveTypeId == filter.LeaveTypeId.Value);
        }

        var list = await query
            .OrderBy(x => x.DepartmentName)
            .ThenBy(x => x.FullName)
            .ThenBy(x => x.LeaveTypeName)
            .Select(x => new LeaveBalanceReportItemDto(
                x.EmployeeCode,
                x.FullName,
                x.DepartmentName,
                x.LeaveTypeName,
                x.Year,
                x.Entitled,
                x.Used,
                x.Remaining
            ))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<IReadOnlyList<LeaveRecordReportItemDto>> GetLeaveRecordsReportAsync(LeaveRecordReportFilter filter, CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var fromDate = filter.FromDate ?? new DateOnly(today.Year, 1, 1);
        var toDate = filter.ToDate ?? new DateOnly(today.Year, 12, 31);

        var query = from r in _context.LeaveRecords.AsNoTracking()
                    join e in _context.Employees.AsNoTracking() on r.EmployeeId equals e.EmployeeId
                    join dept in _context.Departments.AsNoTracking() on e.DepartmentId equals dept.DepartmentId
                    join lt in _context.LeaveTypes.AsNoTracking() on r.LeaveTypeId equals lt.LeaveTypeId
                    where r.StartDate <= toDate && r.EndDate >= fromDate
                    select new
                    {
                        e.EmployeeId,
                        e.EmployeeCode,
                        FullName = e.FirstName + " " + e.LastName,
                        e.DepartmentId,
                        DepartmentName = dept.Name,
                        r.LeaveTypeId,
                        LeaveTypeName = lt.Name,
                        r.StartDate,
                        r.EndDate,
                        r.TotalDays,
                        r.IsHalfDay,
                        r.Status,
                        r.Reason,
                        r.CreatedAtUtc
                    };

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.LeaveTypeId.HasValue)
        {
            query = query.Where(x => x.LeaveTypeId == filter.LeaveTypeId.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        var list = await query
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.EmployeeCode)
            .Select(x => new LeaveRecordReportItemDto(
                x.EmployeeCode,
                x.FullName,
                x.DepartmentName,
                x.LeaveTypeName,
                x.StartDate,
                x.EndDate,
                x.TotalDays,
                x.IsHalfDay,
                x.Status,
                x.Reason,
                x.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<IReadOnlyList<EmployeeReportItemDto>> GetEmployeeReportAsync(EmployeeReportFilter filter, CancellationToken cancellationToken = default)
    {
        var query = from e in _context.Employees.AsNoTracking()
                    join dept in _context.Departments.AsNoTracking() on e.DepartmentId equals dept.DepartmentId
                    join desig in _context.Designations.AsNoTracking() on e.DesignationId equals desig.DesignationId
                    join g in _context.Grades.AsNoTracking() on e.GradeId equals g.GradeId
                    select new
                    {
                        e.EmployeeId,
                        e.EmployeeCode,
                        FullName = e.FirstName + " " + e.LastName,
                        e.Email,
                        e.Phone,
                        e.DateOfBirth,
                        e.Gender,
                        e.DepartmentId,
                        DepartmentName = dept.Name,
                        e.DesignationId,
                        DesignationTitle = desig.Title,
                        GradeCode = g.Code,
                        Salary = g.Salary,
                        e.JoinDate,
                        e.EmploymentType,
                        e.Status,
                        e.ExitDate
                    };

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.DesignationId.HasValue)
        {
            query = query.Where(x => x.DesignationId == filter.DesignationId.Value);
        }

        if (filter.EmploymentType.HasValue)
        {
            query = query.Where(x => x.EmploymentType == filter.EmploymentType.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        var list = await query
            .OrderBy(x => x.DepartmentName)
            .ThenBy(x => x.FullName)
            .Select(x => new EmployeeReportItemDto(
                x.EmployeeCode,
                x.FullName,
                x.Email ?? "",
                x.Phone ?? "",
                x.DateOfBirth,
                x.Gender,
                x.DepartmentName,
                x.DesignationTitle,
                x.GradeCode,
                x.Salary,
                x.JoinDate,
                x.EmploymentType,
                x.Status,
                x.ExitDate
            ))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<IReadOnlyList<AssetReportItemDto>> GetAssetReportAsync(AssetReportFilter filter, CancellationToken cancellationToken = default)
    {
        var query = from a in _context.Assets.AsNoTracking()
                    join assign in _context.AssetAssignments.AsNoTracking().Where(asg => asg.ReturnedAtUtc == null)
                        on a.AssetId equals assign.AssetId into assignJoin
                    from openAssign in assignJoin.DefaultIfEmpty()
                    join e in _context.Employees.AsNoTracking() on openAssign.EmployeeId equals e.EmployeeId into empJoin
                    from emp in empJoin.DefaultIfEmpty()
                    join d in _context.Departments.AsNoTracking() on emp.DepartmentId equals d.DepartmentId into deptJoin
                    from dept in deptJoin.DefaultIfEmpty()
                    select new
                    {
                        a.AssetTag,
                        a.Name,
                        a.Category,
                        a.SerialNumber,
                        a.PurchaseDate,
                        a.Condition,
                        a.Status,
                        AssignedToEmployeeCode = emp != null ? emp.EmployeeCode : null,
                        AssignedToFullName = emp != null ? (emp.FirstName + " " + emp.LastName) : null,
                        AssignedToDepartment = dept != null ? dept.Name : null,
                        DepartmentId = emp != null ? emp.DepartmentId : (Guid?)null,
                        AssignedAtUtc = openAssign != null ? (DateTime?)openAssign.AssignedAtUtc : null
                    };

        if (filter.Category.HasValue)
        {
            query = query.Where(x => x.Category == filter.Category.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x => x.Status == filter.Status.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(x => x.DepartmentId == filter.DepartmentId.Value);
        }

        var list = await query
            .OrderBy(x => x.Category)
            .ThenBy(x => x.AssetTag)
            .Select(x => new AssetReportItemDto(
                x.AssetTag,
                x.Name,
                x.Category,
                x.SerialNumber,
                x.PurchaseDate,
                x.Condition,
                x.Status,
                x.AssignedToEmployeeCode,
                x.AssignedToFullName,
                x.AssignedToDepartment,
                x.AssignedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return list;
    }

    public async Task<EmployeeDashboardDto?> GetEmployeeDashboardAsync(
        Guid employeeId,
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.TodayLocal;
        var nowLocal = _clock.ToLocal(_clock.UtcNow);
        var targetYear = year ?? today.Year;
        var targetMonth = month ?? today.Month;
        var monthStart = new DateOnly(targetYear, targetMonth, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        // 1. Employee Details
        var employee = await _context.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);

        if (employee == null)
            return null;

        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DepartmentId == employee.DepartmentId, cancellationToken);

        var designation = await _context.Designations
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.DesignationId == employee.DesignationId, cancellationToken);

        var fullName = $"{employee.FirstName} {employee.LastName}".Trim();
        var avatarText = !string.IsNullOrEmpty(employee.FirstName) ? employee.FirstName[0].ToString().ToUpper() : "E";

        // 2. Current Shift Assignment
        var shiftAssignment = await _context.ShiftAssignments
            .AsNoTracking()
            .Where(s => s.EmployeeId == employeeId && s.EffectiveFrom <= today && (s.EffectiveTo == null || s.EffectiveTo >= today))
            .OrderByDescending(s => s.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        string shiftName = "Standard Shift";
        string shiftTimingFormatted = "09:00 AM - 06:00 PM";
        TimeOnly shiftStartTime = new TimeOnly(9, 0);
        TimeOnly shiftEndTime = new TimeOnly(18, 0);
        int shiftTargetMinutes = 540; // 9 hours

        if (shiftAssignment != null)
        {
            var shift = await _context.Shifts
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ShiftId == shiftAssignment.ShiftId, cancellationToken);
            if (shift != null)
            {
                shiftName = shift.Name;
                var shiftVersion = await _context.ShiftVersions
                    .AsNoTracking()
                    .Where(v => v.ShiftId == shift.ShiftId && (v.EffectiveTo == null || v.EffectiveTo >= today))
                    .OrderByDescending(v => v.EffectiveFrom)
                    .FirstOrDefaultAsync(cancellationToken);

                if (shiftVersion != null)
                {
                    shiftStartTime = TimeOnly.FromTimeSpan(shiftVersion.StartTime);
                    shiftEndTime = TimeOnly.FromTimeSpan(shiftVersion.EndTime);
                    shiftTimingFormatted = $"{shiftStartTime:hh\\:mm tt} - {shiftEndTime:hh\\:mm tt}";
                    var duration = (shiftEndTime - shiftStartTime).TotalMinutes;
                    if (duration < 0) duration += 24 * 60;
                    shiftTargetMinutes = (int)duration;
                }
            }
        }

        // 3. Today's Attendance
        var todayAttendance = await _context.AttendanceDays
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.WorkDate == today, cancellationToken);

        var dayOfWeekByte = (byte)today.DayOfWeek;
        var isWeekend = await _context.CompanyWorkingDays
            .AsNoTracking()
            .AnyAsync(w => w.DayOfWeek == dayOfWeekByte && !w.IsWorkingDay, cancellationToken);

        var holidayToday = await _context.Holidays
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.HolidayDate == today, cancellationToken);

        string todayStatusText = "Not Checked In";
        AttendanceStatus? todayStatus = todayAttendance?.Status;
        string? checkInFormatted = null;
        string? checkOutFormatted = null;
        int workedMinutes = todayAttendance?.WorkedMinutes ?? 0;
        int progressPercent = 0;
        string timeLeftFormatted = "Shift not started";
        bool isCheckedIn = false;
        bool isCheckedOut = false;
        string note = todayAttendance?.Note ?? string.Empty;

        if (todayAttendance != null)
        {
            todayStatusText = todayAttendance.Status.ToString();
            if (todayAttendance.CheckInUtc.HasValue)
            {
                isCheckedIn = true;
                var localCheckIn = _clock.ToLocal(todayAttendance.CheckInUtc.Value);
                checkInFormatted = localCheckIn.ToString("hh:mm tt");
            }
            if (todayAttendance.CheckOutUtc.HasValue)
            {
                isCheckedOut = true;
                var localCheckOut = _clock.ToLocal(todayAttendance.CheckOutUtc.Value);
                checkOutFormatted = localCheckOut.ToString("hh:mm tt");
            }

            if (isCheckedIn && !isCheckedOut)
            {
                var elapsedMinutes = (int)(_clock.UtcNow - todayAttendance.CheckInUtc!.Value).TotalMinutes;
                if (elapsedMinutes > 0)
                {
                    workedMinutes = elapsedMinutes;
                    progressPercent = Math.Clamp((int)((workedMinutes / (double)shiftTargetMinutes) * 100), 0, 100);
                    var remaining = shiftTargetMinutes - workedMinutes;
                    if (remaining > 0)
                        timeLeftFormatted = $"{remaining / 60}h {remaining % 60}min";
                    else
                        timeLeftFormatted = "Shift completed";
                }
            }
            else if (isCheckedOut)
            {
                progressPercent = 100;
                timeLeftFormatted = "Completed";
            }
        }
        else if (holidayToday != null)
        {
            todayStatusText = $"Holiday ({holidayToday.Name})";
            todayStatus = AttendanceStatus.Holiday;
            timeLeftFormatted = "Holiday";
        }
        else if (isWeekend)
        {
            todayStatusText = "Weekend Off";
            todayStatus = AttendanceStatus.Weekend;
            timeLeftFormatted = "Weekend";
        }

        string workedHoursFormatted = $"{workedMinutes / 60}h {workedMinutes % 60}m";

        var todayDto = new EmployeeTodayAttendanceDto(
            todayStatus,
            todayStatusText,
            checkInFormatted,
            checkOutFormatted,
            workedMinutes,
            workedHoursFormatted,
            timeLeftFormatted,
            progressPercent,
            isCheckedIn,
            isCheckedOut,
            note
        );

        // 4. Shift Overview for Month
        var monthAttendance = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate >= monthStart && a.WorkDate <= monthEnd)
            .ToListAsync(cancellationToken);

        int presentDays = monthAttendance.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late || a.Status == AttendanceStatus.HalfDay);
        int onTimeDays = monthAttendance.Count(a => a.Status == AttendanceStatus.Present && a.LateMinutes == 0);
        decimal onTimeRate = (presentDays > 0) ? Math.Round((decimal)onTimeDays / presentDays * 100, 1) : 100m;

        int totalWorkedMinutesInMonth = monthAttendance.Where(a => a.WorkedMinutes > 0).Sum(a => a.WorkedMinutes);
        int daysWithWorkedMinutes = monthAttendance.Count(a => a.WorkedMinutes > 0);
        int avgWorkedMinutes = daysWithWorkedMinutes > 0 ? totalWorkedMinutesInMonth / daysWithWorkedMinutes : shiftTargetMinutes;
        string avgHoursFormatted = $"{avgWorkedMinutes / 60}h {avgWorkedMinutes % 60}m";

        var checkIns = monthAttendance.Where(a => a.CheckInUtc.HasValue).Select(a => _clock.ToLocal(a.CheckInUtc!.Value).TimeOfDay).ToList();
        string avgCheckInFormatted = checkIns.Count > 0
            ? new TimeOnly((long)checkIns.Average(t => t.Ticks)).ToString("hh:mm tt")
            : shiftStartTime.ToString("hh:mm tt");

        var checkOuts = monthAttendance.Where(a => a.CheckOutUtc.HasValue).Select(a => _clock.ToLocal(a.CheckOutUtc!.Value).TimeOfDay).ToList();
        string avgCheckOutFormatted = checkOuts.Count > 0
            ? new TimeOnly((long)checkOuts.Average(t => t.Ticks)).ToString("hh:mm tt")
            : shiftEndTime.ToString("hh:mm tt");

        var allWorkingDayConfigs = await _context.CompanyWorkingDays.AsNoTracking().ToListAsync(cancellationToken);
        var offDayOfWeekBytes = allWorkingDayConfigs.Where(w => !w.IsWorkingDay).Select(w => w.DayOfWeek).ToHashSet();
        var holidaysInMonth = (await _context.Holidays
            .AsNoTracking()
            .Where(h => h.HolidayDate >= monthStart && h.HolidayDate <= monthEnd)
            .Select(h => h.HolidayDate)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        int totalWorkingDaysThisMonth = 0;
        for (var d = monthStart; d <= monthEnd; d = d.AddDays(1))
        {
            if (!offDayOfWeekBytes.Contains((byte)d.DayOfWeek) && !holidaysInMonth.Contains(d))
            {
                totalWorkingDaysThisMonth++;
            }
        }

        var shiftOverviewDto = new EmployeeShiftOverviewDto(
            shiftName,
            shiftTimingFormatted,
            avgHoursFormatted,
            avgCheckInFormatted,
            avgCheckOutFormatted,
            onTimeRate,
            presentDays,
            totalWorkingDaysThisMonth,
            $"On-time arrival rate: {onTimeRate}% this month."
        );

        // 5. Leave Entitlement & Summary (Donut Chart)
        var balances = await (from b in _context.LeaveBalances.AsNoTracking()
                              join t in _context.LeaveTypes.AsNoTracking() on b.LeaveTypeId equals t.LeaveTypeId
                              where b.EmployeeId == employeeId && b.Year == today.Year
                              select new { t.Name, t.Code, b.Entitled, b.Used, b.Remaining })
                              .ToListAsync(cancellationToken);

        decimal totalEntitled = balances.Sum(b => b.Entitled);
        decimal totalUsed = balances.Sum(b => b.Used);
        decimal totalRemaining = balances.Sum(b => b.Remaining);
        int remainingPct = totalEntitled > 0 ? (int)Math.Round((totalRemaining / totalEntitled) * 100) : 100;

        var colorClasses = new[] { "text-primary", "text-success", "text-warning", "text-info", "text-danger" };
        var balanceDtos = balances.Select((b, i) => new EmployeeLeaveTypeBalanceDto(
            b.Name,
            b.Code,
            b.Entitled,
            b.Used,
            b.Remaining,
            colorClasses[i % colorClasses.Length]
        )).ToList();

        int pendingRequestsCount = await _context.LeaveRecords
            .AsNoTracking()
            .CountAsync(r => r.EmployeeId == employeeId && r.Status == LeaveRecordStatus.Pending, cancellationToken);

        var leaveSummaryDto = new EmployeeLeaveSummaryDto(
            totalEntitled,
            totalUsed,
            totalRemaining,
            remainingPct,
            balanceDtos,
            pendingRequestsCount
        );

        // 6. My Team (Department Peers)
        var peers = await (from p in _context.Employees.AsNoTracking()
                           join deg in _context.Designations.AsNoTracking() on p.DesignationId equals deg.DesignationId into degGroup
                           from deg in degGroup.DefaultIfEmpty()
                           where p.DepartmentId == employee.DepartmentId && p.Status == EmployeeStatus.Active
                           orderby p.EmployeeId == employeeId ? 0 : 1, p.FirstName
                           select new
                           {
                               p.EmployeeId,
                               FullName = (p.FirstName + " " + p.LastName).Trim(),
                               Designation = deg != null ? deg.Title : "Staff",
                               p.EmployeeCode
                           })
                           .Take(6)
                           .ToListAsync(cancellationToken);

        var peerIds = peers.Select(p => p.EmployeeId).ToList();
        var peerTodayAttendance = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => peerIds.Contains(a.EmployeeId) && a.WorkDate == today)
            .ToListAsync(cancellationToken);

        int currentDayOfWeekOffset = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        if (currentDayOfWeekOffset < 0) currentDayOfWeekOffset += 7;
        var weekMonday = today.AddDays(-currentDayOfWeekOffset);
        var weekFriday = weekMonday.AddDays(4);

        var peerWeekAttendance = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => peerIds.Contains(a.EmployeeId) && a.WorkDate >= weekMonday && a.WorkDate <= weekFriday)
            .ToListAsync(cancellationToken);

        var teamList = new List<EmployeeTeamMemberDto>();
        foreach (var p in peers)
        {
            var att = peerTodayAttendance.FirstOrDefault(a => a.EmployeeId == p.EmployeeId);
            var status = att?.Status;
            string statusText = "In office";
            string badgeClass = "badge-soft-success";

            if (status == AttendanceStatus.OnLeave)
            {
                statusText = "On leave";
                badgeClass = "badge-soft-primary";
            }
            else if (status == AttendanceStatus.Absent)
            {
                statusText = "Absent";
                badgeClass = "badge-soft-danger";
            }
            else if (status == AttendanceStatus.Late)
            {
                statusText = "Late";
                badgeClass = "badge-soft-warning";
            }
            else if (status == AttendanceStatus.Weekend || status == AttendanceStatus.Holiday)
            {
                statusText = status?.ToString() ?? "Unknown";
                badgeClass = "badge-soft-secondary";
            }
            else if (status == null)
            {
                statusText = isWeekend ? "Weekend" : (holidayToday != null ? "Holiday" : "Not yet in");
                badgeClass = "badge-soft-secondary";
            }

            var weeklyCodes = new Dictionary<int, string>();
            for (int dayIdx = 0; dayIdx < 5; dayIdx++)
            {
                var dayDate = weekMonday.AddDays(dayIdx);
                var dayAtt = peerWeekAttendance.FirstOrDefault(a => a.EmployeeId == p.EmployeeId && a.WorkDate == dayDate);
                if (dayAtt != null)
                {
                    weeklyCodes[dayIdx] = dayAtt.Status switch
                    {
                        AttendanceStatus.Present => "P",
                        AttendanceStatus.Late => "P",
                        AttendanceStatus.OnLeave => "L",
                        AttendanceStatus.Absent => "A",
                        AttendanceStatus.Weekend => "W",
                        AttendanceStatus.Holiday => "H",
                        _ => "-"
                    };
                }
                else
                {
                    weeklyCodes[dayIdx] = dayDate > today ? "-" : "P";
                }
            }

            teamList.Add(new EmployeeTeamMemberDto(
                p.EmployeeId,
                p.FullName,
                p.Designation,
                !string.IsNullOrEmpty(p.FullName) ? p.FullName[0].ToString().ToUpper() : "T",
                status,
                statusText,
                badgeClass,
                weeklyCodes
            ));
        }

        // 7. Interactive Calendar Data
        var weeklyOffDays = allWorkingDayConfigs
            .Where(w => !w.IsWorkingDay)
            .Select(w => (DayOfWeek)w.DayOfWeek)
            .ToList();

        var holidaysMap = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.HolidayDate >= monthStart && h.HolidayDate <= monthEnd)
            .ToDictionaryAsync(h => h.HolidayDate, h => h.Name, cancellationToken);

        var attendanceByDate = new Dictionary<DateOnly, EmployeeCalendarDayDto>();
        int totalDays = DateTime.DaysInMonth(targetYear, targetMonth);
        int firstDayOffset = (int)monthStart.DayOfWeek;

        foreach (var att in monthAttendance)
        {
            var isDayWeekend = weeklyOffDays.Contains(att.WorkDate.DayOfWeek);
            var isDayHoliday = holidaysMap.ContainsKey(att.WorkDate);
            string? hName = isDayHoliday ? holidaysMap[att.WorkDate] : null;

            attendanceByDate[att.WorkDate] = new EmployeeCalendarDayDto(
                att.WorkDate,
                att.Status,
                att.Status.ToString(),
                att.CheckInUtc.HasValue ? _clock.ToLocal(att.CheckInUtc.Value).ToString("hh:mm tt") : null,
                att.CheckOutUtc.HasValue ? _clock.ToLocal(att.CheckOutUtc.Value).ToString("hh:mm tt") : null,
                $"{att.WorkedMinutes / 60}h {att.WorkedMinutes % 60}m",
                att.WorkDate == today,
                isDayWeekend,
                isDayHoliday,
                hName
            );
        }

        var calendarDto = new EmployeeCalendarDto(
            targetYear,
            targetMonth,
            monthStart.ToString("MMMM yyyy"),
            totalDays,
            firstDayOffset,
            weeklyOffDays,
            holidaysMap,
            attendanceByDate
        );

        // 8. Working History
        var recentRows = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.WorkDate <= today)
            .OrderByDescending(a => a.WorkDate)
            .Take(7)
            .ToListAsync(cancellationToken);

        var workingHistory = new List<EmployeeRecentAttendanceDto>();
        foreach (var r in recentRows)
        {
            string checkIn = r.CheckInUtc.HasValue ? _clock.ToLocal(r.CheckInUtc.Value).ToString("hh:mm tt") : "--";
            string checkOut = r.CheckOutUtc.HasValue ? _clock.ToLocal(r.CheckOutUtc.Value).ToString("hh:mm tt") : "--";
            string effTime = $"{r.WorkedMinutes / 60}:{r.WorkedMinutes % 60:D2} hrs";

            int diff = r.WorkedMinutes - shiftTargetMinutes;
            bool isOnTarget = diff >= -15;
            string targetText = isOnTarget
                ? (diff > 15 ? $"Over {diff}m" : "On target")
                : $"Under {Math.Abs(diff)}m";

            workingHistory.Add(new EmployeeRecentAttendanceDto(
                r.WorkDate,
                r.WorkDate.ToString("dd MMM"),
                checkIn,
                checkOut,
                effTime,
                targetText,
                isOnTarget,
                r.Status,
                r.Status.ToString()
            ));
        }

        return new EmployeeDashboardDto(
            employee.EmployeeId,
            fullName,
            employee.EmployeeCode,
            department?.Name ?? "General",
            designation?.Title ?? "Employee",
            avatarText,
            nowLocal.ToString("hh:mm tt"),
            today.ToString("dddd, MMMM dd, yyyy"),
            todayDto,
            shiftOverviewDto,
            leaveSummaryDto,
            teamList,
            calendarDto,
            workingHistory
        );
    }
}
