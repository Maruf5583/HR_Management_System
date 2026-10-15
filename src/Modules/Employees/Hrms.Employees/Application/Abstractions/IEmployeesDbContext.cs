using Hrms.Employees.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Hrms.Employees.Application.Abstractions;

public interface IEmployeesDbContext
{
    DbSet<Department> Departments { get; }
    DbSet<Designation> Designations { get; }
    DbSet<Grade> Grades { get; }
    DbSet<Employee> Employees { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
