using System.Globalization;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Domain.Entities;
using Hrms.Settings.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IEmployeesDbContext _context;
    private readonly ISettingsReader _settingsReader;

    public EmployeeRepository(IEmployeesDbContext context, ISettingsReader settingsReader)
    {
        _context = context;
        _settingsReader = settingsReader;
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
        var format = await _settingsReader.GetStringAsync(SettingKeys.EmployeeCodeFormat, cancellationToken);
        var prefix = format.Contains('{') ? format.Substring(0, format.IndexOf('{')) : string.Empty;

        var existingCodes = await _context.Employees
            .Select(e => e.EmployeeCode)
            .ToListAsync(cancellationToken);

        int maxNumber = 0;
        foreach (var code in existingCodes)
        {
            if (!string.IsNullOrEmpty(prefix) && code.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var numPart = code.Substring(prefix.Length);
                if (int.TryParse(numPart, out int num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
            else if (string.IsNullOrEmpty(prefix))
            {
                if (int.TryParse(code, out int num) && num > maxNumber)
                {
                    maxNumber = num;
                }
            }
        }

        int next = maxNumber + 1;
        return string.Format(CultureInfo.InvariantCulture, format, next);
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
