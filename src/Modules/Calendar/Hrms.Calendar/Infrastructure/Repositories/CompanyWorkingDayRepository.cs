using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Calendar.Infrastructure.Repositories;

public class CompanyWorkingDayRepository : ICompanyWorkingDayRepository
{
    private readonly ICalendarDbContext _context;

    public CompanyWorkingDayRepository(ICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CompanyWorkingDay>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CompanyWorkingDays
            .OrderBy(d => d.DayOfWeek)
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyWorkingDay?> GetByDayOfWeekAsync(byte dayOfWeek, CancellationToken cancellationToken = default)
    {
        return await _context.CompanyWorkingDays
            .FirstOrDefaultAsync(d => d.DayOfWeek == dayOfWeek, cancellationToken);
    }

    public async Task AddAsync(CompanyWorkingDay workingDay, CancellationToken cancellationToken = default)
    {
        await _context.CompanyWorkingDays.AddAsync(workingDay, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<CompanyWorkingDay> workingDays, CancellationToken cancellationToken = default)
    {
        await _context.CompanyWorkingDays.AddRangeAsync(workingDays, cancellationToken);
    }

    public Task UpdateAsync(CompanyWorkingDay workingDay, CancellationToken cancellationToken = default)
    {
        _context.CompanyWorkingDays.Update(workingDay);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
