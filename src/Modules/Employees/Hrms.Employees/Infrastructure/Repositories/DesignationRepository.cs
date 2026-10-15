using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Repositories;

public class DesignationRepository : IDesignationRepository
{
    private readonly IEmployeesDbContext _context;

    public DesignationRepository(IEmployeesDbContext context)
    {
        _context = context;
    }

    public async Task<Designation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Designations
            .Include(d => d.Department)
            .FirstOrDefaultAsync(d => d.DesignationId == id, cancellationToken);
    }

    public async Task<bool> ExistsByTitleAsync(Guid departmentId, string title, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Designations
            .Where(d => d.DepartmentId == departmentId);

        if (excludeId.HasValue)
        {
            query = query.Where(d => d.DesignationId != excludeId.Value);
        }

        return await query.AnyAsync(d => d.Title.ToLower() == title.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Designation designation, CancellationToken cancellationToken = default)
    {
        await _context.Designations.AddAsync(designation, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
