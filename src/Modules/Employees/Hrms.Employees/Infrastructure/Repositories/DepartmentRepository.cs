using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly IEmployeesDbContext _context;

    public DepartmentRepository(IEmployeesDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.DepartmentId == id, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Departments.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(d => d.DepartmentId != excludeId.Value);
        }

        return await query.AnyAsync(d => d.Name.ToLower() == name.ToLower(), cancellationToken);
    }

    public async Task AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        await _context.Departments.AddAsync(department, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
