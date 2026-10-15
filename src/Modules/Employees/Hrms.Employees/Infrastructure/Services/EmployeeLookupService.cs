using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Contracts;
using Hrms.Employees.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class EmployeeLookupService : IEmployeeLookup
{
    private readonly IEmployeesDbContext _context;

    public EmployeeLookupService(IEmployeesDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .AnyAsync(e => e.EmployeeId == employeeId, cancellationToken);
    }

    public async Task<EmployeeBasicDto?> GetBasicInfoAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(e => e.Grade)
            .Where(e => e.EmployeeId == employeeId)
            .Select(e => new EmployeeBasicDto(
                e.EmployeeId,
                e.EmployeeCode,
                e.FirstName,
                e.LastName,
                e.Email,
                e.Status.ToString(),
                e.JoinDate,
                e.ExitDate,
                e.DepartmentId,
                e.Grade != null ? e.Grade.Salary : 0m
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmployeeLookupDto>> GetActiveEmployeesDropdownAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .AsNoTracking()
            .Where(e => e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnNotice)
            .OrderBy(e => e.EmployeeCode)
            .Select(e => new EmployeeLookupDto(
                e.EmployeeId,
                e.EmployeeCode,
                (e.FirstName + " " + e.LastName).Trim(),
                e.Email
            ))
            .ToListAsync(cancellationToken);
    }
}
