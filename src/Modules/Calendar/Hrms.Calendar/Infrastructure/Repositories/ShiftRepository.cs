using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Hrms.Calendar.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Calendar.Infrastructure.Repositories;

public class ShiftRepository : IShiftRepository
{
    private readonly ICalendarDbContext _context;

    public ShiftRepository(ICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Shift>> GetAllAsync(ShiftStatus? statusFilter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Shifts
            .Include(s => s.Versions)
            .Include(s => s.Assignments)
            .AsQueryable();

        if (statusFilter.HasValue)
        {
            query = query.Where(s => s.Status == statusFilter.Value);
        }

        return await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);
    }

    public async Task<Shift?> GetByIdAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.Shifts
            .Include(s => s.Versions)
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.ShiftId == shiftId, cancellationToken);
    }

    public async Task<Shift?> GetByIdWithVersionsAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.Shifts
            .Include(s => s.Versions.OrderByDescending(v => v.EffectiveFrom))
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.ShiftId == shiftId, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeShiftId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Shifts.AsQueryable();
        if (excludeShiftId.HasValue)
        {
            query = query.Where(s => s.ShiftId != excludeShiftId.Value);
        }

        return await query.AnyAsync(s => s.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Shift shift, CancellationToken cancellationToken = default)
    {
        await _context.Shifts.AddAsync(shift, cancellationToken);
    }

    public async Task AddVersionAsync(ShiftVersion version, CancellationToken cancellationToken = default)
    {
        await _context.ShiftVersions.AddAsync(version, cancellationToken);
    }

    public async Task<ShiftVersion?> GetLatestVersionAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftVersions
            .Where(v => v.ShiftId == shiftId)
            .OrderByDescending(v => v.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShiftVersion?> GetVersionForDateAsync(Guid shiftId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftVersions
            .Where(v => v.ShiftId == shiftId && v.EffectiveFrom <= date && (v.EffectiveTo == null || v.EffectiveTo >= date))
            .OrderByDescending(v => v.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Delete(Shift shift)
    {
        var versions = _context.ShiftVersions.Where(v => v.ShiftId == shift.ShiftId).ToList();
        if (versions.Any())
        {
            _context.ShiftVersions.RemoveRange(versions);
        }

        _context.Shifts.Remove(shift);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
