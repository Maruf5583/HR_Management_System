namespace Hrms.Calendar.Contracts.Models;

public record DayScheduleDto(
    DateOnly Date,
    bool IsWorkingDay,
    bool IsWeeklyOff,
    bool IsHoliday,
    string? HolidayName,
    ShiftDetailsDto? Shift
);
