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
}
