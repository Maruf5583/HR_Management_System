using Hrms.Calendar.Contracts.Models;

namespace Hrms.Calendar.Contracts;

public interface ICalendarQuery
{
    Task<bool> IsWorkingDayAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> IsHolidayAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<HolidayDto?> GetHolidayAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<ShiftDetailsDto?> GetShiftForEmployeeAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
    Task<DayScheduleDto> GetDayScheduleAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default);
}
