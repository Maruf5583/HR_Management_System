using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Calendar.Infrastructure.Repositories;

public class HolidayRepository : IHolidayRepository
{
    private readonly ICalendarDbContext _context;

    public HolidayRepository(ICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Holiday>> GetAllAsync(int? year = null, HolidayType? type = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Holidays.AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(h => h.HolidayDate.Year == year.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(h => h.Type == type.Value);
        }

        return await query.OrderBy(h => h.HolidayDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Holiday>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .Where(h => h.HolidayDate >= startDate && h.HolidayDate <= endDate)
            .OrderBy(h => h.HolidayDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<Holiday?> GetByIdAsync(Guid holidayId, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .FirstOrDefaultAsync(h => h.HolidayId == holidayId, cancellationToken);
    }

    public async Task<Holiday?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.Holidays
            .FirstOrDefaultAsync(h => h.HolidayDate == date, cancellationToken);
    }

    public async Task<bool> ExistsOnDateAsync(DateOnly date, Guid? excludeHolidayId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Holidays.Where(h => h.HolidayDate == date);

        if (excludeHolidayId.HasValue)
        {
            query = query.Where(h => h.HolidayId != excludeHolidayId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        await _context.Holidays.AddAsync(holiday, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Holiday> holidays, CancellationToken cancellationToken = default)
    {
        await _context.Holidays.AddRangeAsync(holidays, cancellationToken);
    }

    public Task DeleteAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        _context.Holidays.Remove(holiday);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IEnumerable<Holiday> holidays, CancellationToken cancellationToken = default)
    {
        _context.Holidays.RemoveRange(holidays);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
