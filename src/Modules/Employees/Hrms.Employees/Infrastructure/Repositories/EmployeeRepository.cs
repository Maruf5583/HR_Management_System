using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private const string CodePrefix = "EMP-";
    private readonly IEmployeesDbContext _context;

    public EmployeeRepository(IEmployeesDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == id, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Employees
            .Where(e => e.Email != null);

        if (excludeId.HasValue)
        {
            query = query.Where(e => e.EmployeeId != excludeId.Value);
        }

        return await query.AnyAsync(e => e.Email!.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<string> GetNextEmployeeCodeAsync(CancellationToken cancellationToken = default)
    {
        var existingCodes = await _context.Employees
            .Select(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        int maxNumber = 0;
        foreach (var code in existingCodes)
        {
            if (code.StartsWith(CodePrefix, StringComparison.OrdinalIgnoreCase))
            {
                var numPart = code.Substring(CodePrefix.Length);
                if (int.TryParse(numPart, out int num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
        }

        int next = maxNumber + 1;
        return $"{CodePrefix}{next:D4}";
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
