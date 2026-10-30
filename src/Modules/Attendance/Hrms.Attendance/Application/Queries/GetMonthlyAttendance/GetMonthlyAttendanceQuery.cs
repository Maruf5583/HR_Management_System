using Hrms.Attendance.Application.Abstractions;
using Hrms.Attendance.Application.Models;
using Hrms.Attendance.Domain.Enums;
using Hrms.Employees.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Attendance.Application.Queries.GetMonthlyAttendance;

public record GetMonthlyAttendanceQuery(
    int Year,
    int Month,
    string? SearchTerm = null
);

public interface IGetMonthlyAttendanceHandler
{
    Task<MonthlyAttendanceDto> HandleAsync(GetMonthlyAttendanceQuery query, CancellationToken cancellationToken = default);
}

public class GetMonthlyAttendanceHandler : IGetMonthlyAttendanceHandler
{
    private readonly IAttendanceDbContext _context;
    private readonly IEmployeeLookup _employeeLookup;

    public GetMonthlyAttendanceHandler(
        IAttendanceDbContext context,
        IEmployeeLookup employeeLookup)
    {
        _context = context;
        _employeeLookup = employeeLookup;
    }

    public async Task<MonthlyAttendanceDto> HandleAsync(GetMonthlyAttendanceQuery query, CancellationToken cancellationToken = default)
    {
        var year = query.Year;
        var month = query.Month;
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var firstDay = new DateOnly(year, month, 1);
        var lastDay = new DateOnly(year, month, daysInMonth);

        var activeEmployees = await _employeeLookup.GetActiveEmployeesDropdownAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var search = query.SearchTerm.Trim().ToLowerInvariant();
            activeEmployees = activeEmployees
                .Where(e => e.FullName.ToLowerInvariant().Contains(search) || e.EmployeeCode.ToLowerInvariant().Contains(search))
                .ToList();
        }

        var records = await _context.AttendanceDays
            .AsNoTracking()
            .Where(a => a.WorkDate >= firstDay && a.WorkDate <= lastDay)
            .ToListAsync(cancellationToken);

        var recordsByEmployee = records
            .GroupBy(a => a.EmployeeId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(a => a.WorkDate.Day, a => a.Status));

        var employeeDtos = new List<MonthlyEmployeeAttendanceDto>();

        foreach (var emp in activeEmployees.OrderBy(e => e.EmployeeCode))
        {
            var dailyMap = new Dictionary<int, AttendanceStatus?>();
            int present = 0, late = 0, halfDay = 0, absent = 0, onLeave = 0, holiday = 0, weekend = 0;

            if (recordsByEmployee.TryGetValue(emp.EmployeeId, out var empDays))
            {
                for (int day = 1; day <= daysInMonth; day++)
                {
                    if (empDays.TryGetValue(day, out var status))
                    {
                        dailyMap[day] = status;
                        switch (status)
                        {
                            case AttendanceStatus.Present: present++; break;
                            case AttendanceStatus.Late: late++; break;
                            case AttendanceStatus.HalfDay: halfDay++; break;
                            case AttendanceStatus.Absent: absent++; break;
                            case AttendanceStatus.OnLeave: onLeave++; break;
                            case AttendanceStatus.Holiday: holiday++; break;
                            case AttendanceStatus.Weekend: weekend++; break;
                        }
                    }
                    else
                    {
                        dailyMap[day] = null;
                    }
                }
            }
            else
            {
                for (int day = 1; day <= daysInMonth; day++)
                {
                    dailyMap[day] = null;
                }
            }

            employeeDtos.Add(new MonthlyEmployeeAttendanceDto(
                EmployeeId: emp.EmployeeId,
                EmployeeCode: emp.EmployeeCode,
                FullName: emp.FullName,
                DailyStatuses: dailyMap,
                PresentCount: present,
                LateCount: late,
                HalfDayCount: halfDay,
                AbsentCount: absent,
                OnLeaveCount: onLeave,
                HolidayCount: holiday,
                WeekendCount: weekend
            ));
        }

        return new MonthlyAttendanceDto(
            Year: year,
            Month: month,
            DaysInMonth: daysInMonth,
            Employees: employeeDtos
        );
    }
}
