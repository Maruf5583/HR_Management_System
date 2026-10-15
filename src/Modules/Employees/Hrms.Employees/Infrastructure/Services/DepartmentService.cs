using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;
    private readonly IEmployeesDbContext _context;
    private readonly IValidator<CreateDepartmentModel> _createValidator;
    private readonly IValidator<UpdateDepartmentModel> _updateValidator;

    public DepartmentService(
        IDepartmentRepository repository,
        IEmployeesDbContext context,
        IValidator<CreateDepartmentModel> createValidator,
        IValidator<UpdateDepartmentModel> updateValidator)
    {
        _repository = repository;
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<DepartmentDto>> GetDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.DepartmentId,
                d.Name,
                d.Status,
                d.Designations.Count,
                _context.Employees.Count(e => e.DepartmentId == d.DepartmentId),
                d.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto?> GetDepartmentByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .Where(d => d.DepartmentId == id)
            .Select(d => new DepartmentDto(
                d.DepartmentId,
                d.Name,
                d.Status,
                d.Designations.Count,
                _context.Employees.Count(e => e.DepartmentId == d.DepartmentId),
                d.CreatedAtUtc
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateDepartmentAsync(CreateDepartmentModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        if (await _repository.ExistsByNameAsync(model.Name.Trim(), null, cancellationToken))
        {
            return Result<Guid>.Failure(new Error("Department.DuplicateName", $"Department '{model.Name.Trim()}' already exists."));
        }

        var department = Department.Create(model.Name.Trim());
        await _repository.AddAsync(department, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.DepartmentId);
    }

    public async Task<Result> UpdateDepartmentAsync(UpdateDepartmentModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var department = await _repository.GetByIdAsync(model.Id, cancellationToken);
        if (department == null)
        {
            return Result.Failure(new Error("Department.NotFound", "Department not found."));
        }

        if (await _repository.ExistsByNameAsync(model.Name.Trim(), model.Id, cancellationToken))
        {
            return Result.Failure(new Error("Department.DuplicateName", $"Department '{model.Name.Trim()}' already exists."));
        }

        department.Update(model.Name.Trim());
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetDepartmentStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default)
    {
        var department = await _repository.GetByIdAsync(id, cancellationToken);
        if (department == null)
        {
            return Result.Failure(new Error("Department.NotFound", "Department not found."));
        }

        if (status == EntityStatus.Inactive)
        {
            var hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.DepartmentId == id && (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnNotice), cancellationToken);

            if (hasActiveEmployees)
            {
                return Result.Failure(new Error("Department.HasActiveEmployees", "Cannot deactivate department while active employees belong to it."));
            }

            department.Deactivate();
        }
        else
        {
            department.Activate();
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
