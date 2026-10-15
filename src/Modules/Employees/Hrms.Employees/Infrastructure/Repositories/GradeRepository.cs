using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Repositories;

public class GradeRepository : IGradeRepository
{
    private readonly IEmployeesDbContext _context;

    public GradeRepository(IEmployeesDbContext context)
    {
        _context = context;
    }

    public async Task<Grade?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Grades
            .Include(g => g.Designation)
            .FirstOrDefaultAsync(g => g.GradeId == id, cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Grades.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(g => g.GradeId != excludeId.Value);
        }

        return await query.AnyAsync(g => g.Code.ToLower() == code.ToLower(), cancellationToken);
    }

    public async Task<bool> ExistsByLevelAsync(Guid designationId, string level, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Grades
            .Where(g => g.DesignationId == designationId);

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.GradeId != excludeId.Value);
        }

        return await query.AnyAsync(g => g.Level.ToLower() == level.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Grade grade, CancellationToken cancellationToken = default)
    {
        await _context.Grades.AddAsync(grade, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
