using Hrms.Calendar.Contracts;

namespace Hrms.Leave.Application.Services;

public interface ILeaveDaysCalculator
{
    Task<decimal> CalculateTotalDaysAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        CancellationToken cancellationToken = default);
}

public class LeaveDaysCalculator : ILeaveDaysCalculator
{
    private readonly ICalendarQuery _calendarQuery;

    public LeaveDaysCalculator(ICalendarQuery calendarQuery)
    {
        _calendarQuery = calendarQuery;
    }

    public async Task<decimal> CalculateTotalDaysAsync(
        DateOnly startDate,
        DateOnly endDate,
        bool isHalfDay,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
        {
            return 0m;
        }

        if (isHalfDay)
        {
            if (startDate != endDate)
            {
                return 0m;
            }

            var isWorking = await _calendarQuery.IsWorkingDayAsync(startDate, cancellationToken);
            var isHoliday = await _calendarQuery.IsHolidayAsync(startDate, cancellationToken);

            if (isWorking && !isHoliday)
            {
                return 0.5m;
            }

            return 0m;
        }

        decimal count = 0m;
        var current = startDate;
        while (current <= endDate)
        {
            var isWorking = await _calendarQuery.IsWorkingDayAsync(current, cancellationToken);
            var isHoliday = await _calendarQuery.IsHolidayAsync(current, cancellationToken);

            if (isWorking && !isHoliday)
            {
                count += 1.0m;
            }

            current = current.AddDays(1);
        }

        return count;
    }
}
