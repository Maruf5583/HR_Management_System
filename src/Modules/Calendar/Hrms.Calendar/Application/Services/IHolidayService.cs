using Hrms.Calendar.Application.Models;
using Hrms.Calendar.Domain.Enums;
using Hrms.Shared.Primitives;

namespace Hrms.Calendar.Application.Services;

public interface IHolidayService
{
    Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(int? year = null, HolidayType? type = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HolidayDto>> GetHolidaysForRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<HolidayDto?> GetByIdAsync(Guid holidayId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> CreateHolidayAsync(CreateHolidayCommand command, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<Guid>>> CreateMultiDayHolidayAsync(CreateMultiDayHolidayCommand command, CancellationToken cancellationToken = default);
    Task<Result> UpdateHolidayAsync(UpdateHolidayCommand command, CancellationToken cancellationToken = default);
    Task<Result> DeleteHolidayAsync(Guid holidayId, CancellationToken cancellationToken = default);
    Task<Result<int>> DeleteHolidaysMultipleAsync(IEnumerable<Guid> holidayIds, CancellationToken cancellationToken = default);
}
