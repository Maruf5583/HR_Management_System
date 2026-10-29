using Hrms.Calendar.Application.Abstractions;
using Hrms.Calendar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Calendar.Infrastructure.Repositories;

public class ShiftAssignmentRepository : IShiftAssignmentRepository
{
    private readonly ICalendarDbContext _context;

    public ShiftAssignmentRepository(ICalendarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsForEmployeeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShiftAssignment>> GetAssignmentsForShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a => a.ShiftId == shiftId)
            .OrderByDescending(a => a.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<ShiftAssignment?> GetCurrentAssignmentAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.EffectiveTo == null, cancellationToken);
    }

    public async Task<ShiftAssignment?> GetAssignmentForDateAsync(Guid employeeId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.Shift)
            .Where(a => a.EmployeeId == employeeId && a.EffectiveFrom <= date && (a.EffectiveTo == null || a.EffectiveTo >= date))
            .OrderByDescending(a => a.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ShiftAssignment?> GetByIdAsync(Guid shiftAssignmentId, CancellationToken cancellationToken = default)
    {
        return await _context.ShiftAssignments
            .Include(a => a.Shift)
            .FirstOrDefaultAsync(a => a.ShiftAssignmentId == shiftAssignmentId, cancellationToken);
    }

    public async Task<bool> HasOverlappingAssignmentAsync(
        Guid employeeId,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        Guid? excludeAssignmentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ShiftAssignments
            .Where(a => a.EmployeeId == employeeId);

        if (excludeAssignmentId.HasValue)
        {
            query = query.Where(a => a.ShiftAssignmentId != excludeAssignmentId.Value);
        }

        return await query.AnyAsync(a =>
            (a.EffectiveTo == null || a.EffectiveTo >= effectiveFrom) &&
            (effectiveTo == null || a.EffectiveFrom <= effectiveTo.Value),
            cancellationToken);
    }

    public async Task AddAsync(ShiftAssignment assignment, CancellationToken cancellationToken = default)
    {
        await _context.ShiftAssignments.AddAsync(assignment, cancellationToken);
    }

    public void Delete(ShiftAssignment assignment)
    {
        _context.ShiftAssignments.Remove(assignment);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
