using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;

namespace Hrms.Calendar.Application.Abstractions;

public interface IHolidayRepository
{
    Task<IReadOnlyList<Holiday>> GetAllAsync(int? year = null, HolidayType? type = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<Holiday?> GetByIdAsync(Guid holidayId, CancellationToken cancellationToken = default);
    Task<Holiday?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<bool> ExistsOnDateAsync(DateOnly date, Guid? excludeHolidayId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<Holiday> holidays, CancellationToken cancellationToken = default);
    Task DeleteAsync(Holiday holiday, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IEnumerable<Holiday> holidays, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
