using Hrms.Calendar.Domain.Entities;

namespace Hrms.Calendar.Application.Abstractions;

public interface ICompanyWorkingDayRepository
{
    Task<IReadOnlyList<CompanyWorkingDay>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<CompanyWorkingDay?> GetByDayOfWeekAsync(byte dayOfWeek, CancellationToken cancellationToken = default);
    Task AddAsync(CompanyWorkingDay workingDay, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<CompanyWorkingDay> workingDays, CancellationToken cancellationToken = default);
    Task UpdateAsync(CompanyWorkingDay workingDay, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
