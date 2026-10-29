using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Contracts;
using Hrms.Calendar.Contracts.Models;
using Hrms.Settings.Contracts;

namespace Hrms.Calendar.Infrastructure.Services;

public class CalendarQueryService : ICalendarQuery
{
    private readonly ICompanyWorkingDayRepository _workingDayRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IShiftAssignmentRepository _assignmentRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly ISettingsReader _settingsReader;

    public CalendarQueryService(
        ICompanyWorkingDayRepository workingDayRepository,
        IHolidayRepository holidayRepository,
        IShiftAssignmentRepository assignmentRepository,
        IShiftRepository shiftRepository,
        ISettingsReader settingsReader)
    {
        _workingDayRepository = workingDayRepository;
        _holidayRepository = holidayRepository;
        _assignmentRepository = assignmentRepository;
        _shiftRepository = shiftRepository;
        _settingsReader = settingsReader;
    }

    public async Task<bool> IsWorkingDayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var dayOfWeek = (byte)date.DayOfWeek;
        var day = await _workingDayRepository.GetByDayOfWeekAsync(dayOfWeek, cancellationToken);
        return day?.IsWorkingDay ?? (dayOfWeek >= 1 && dayOfWeek <= 5);
    }

    public async Task<bool> IsHolidayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _holidayRepository.ExistsOnDateAsync(date, cancellationToken: cancellationToken);
    }

    public async Task<HolidayDto?> GetHolidayAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var holiday = await _holidayRepository.GetByDateAsync(date, cancellationToken);
        if (holiday == null) return null;

        return new HolidayDto(
            holiday.HolidayId,
            holiday.Name,
            holiday.HolidayDate,
            holiday.Type.ToString()
        );
    }

    public async Task<ShiftDetailsDto?> GetShiftForEmployeeAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignmentRepository.GetAssignmentForDateAsync(employeeId, date, cancellationToken);
        if (assignment == null) return null;

        var shift = assignment.Shift ?? await _shiftRepository.GetByIdAsync(assignment.ShiftId, cancellationToken);
        if (shift == null) return null;

        var version = await _shiftRepository.GetVersionForDateAsync(shift.ShiftId, date, cancellationToken);
        if (version == null) return null;

        return new ShiftDetailsDto(
            shift.ShiftId,
            shift.Name,
            version.StartTime,
            version.EndTime,
            version.GraceMinutes,
            version.BreakMinutes,
            version.IsNightShift
        );
    }

    public async Task<DayScheduleDto> GetDayScheduleAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var isWorkingDay = await IsWorkingDayAsync(date, cancellationToken);
        var isWeeklyOff = !isWorkingDay;

        var holiday = await _holidayRepository.GetByDateAsync(date, cancellationToken);
        var isHoliday = holiday != null;

        if (isHoliday && isWeeklyOff)
        {
            var priority = await _settingsReader.GetStringAsync(SettingKeys.AttendanceHolidayPriority, cancellationToken);
            if (string.Equals(priority, "WeekendOverHoliday", StringComparison.OrdinalIgnoreCase))
            {
                isHoliday = false;
            }
            else
            {
                isWeeklyOff = false;
            }
        }

        var shift = await GetShiftForEmployeeAsync(employeeId, date, cancellationToken);

        return new DayScheduleDto(
            date,
            isWorkingDay && !isHoliday,
            isWeeklyOff,
            isHoliday,
            holiday?.Name,
            shift
        );
    }
}
