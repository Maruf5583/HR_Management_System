using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class DesignationService : IDesignationService
{
    private readonly IDesignationRepository _repository;
    private readonly IDepartmentRepository _deptRepository;
    private readonly IEmployeesDbContext _context;
    private readonly IValidator<CreateDesignationModel> _createValidator;
    private readonly IValidator<UpdateDesignationModel> _updateValidator;

    public DesignationService(
        IDesignationRepository repository,
        IDepartmentRepository deptRepository,
        IEmployeesDbContext context,
        IValidator<CreateDesignationModel> createValidator,
        IValidator<UpdateDesignationModel> updateValidator)
    {
        _repository = repository;
        _deptRepository = deptRepository;
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<DesignationDto>> GetDesignationsAsync(Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Designations
            .AsNoTracking()
            .Include(d => d.Department)
            .AsQueryable();

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(d => d.DepartmentId == departmentId.Value);
        }

        return await query
            .OrderBy(d => d.Department != null ? d.Department.Name : string.Empty)
            .ThenBy(d => d.Title)
            .Select(d => new DesignationDto(
                d.DesignationId,
                d.Title,
                d.DepartmentId,
                d.Department != null ? d.Department.Name : string.Empty,
                d.Status,
                d.Grades.Count,
                _context.Employees.Count(e => e.DesignationId == d.DesignationId),
                d.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<DesignationDto?> GetDesignationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Designations
            .AsNoTracking()
            .Include(d => d.Department)
            .Where(d => d.DesignationId == id)
            .Select(d => new DesignationDto(
                d.DesignationId,
                d.Title,
                d.DepartmentId,
                d.Department != null ? d.Department.Name : string.Empty,
                d.Status,
                d.Grades.Count,
                _context.Employees.Count(e => e.DesignationId == d.DesignationId),
                d.CreatedAtUtc
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateDesignationAsync(CreateDesignationModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var department = await _deptRepository.GetByIdAsync(model.DepartmentId, cancellationToken);
        if (department == null)
        {
            return Result<Guid>.Failure(new Error("Department.NotFound", "Selected department not found."));
        }

        if (department.Status != EntityStatus.Active)
        {
            return Result<Guid>.Failure(new Error("Department.Inactive", "Cannot create a designation under an inactive department."));
        }

        if (await _repository.ExistsByTitleAsync(model.DepartmentId, model.Title.Trim(), null, cancellationToken))
        {
            return Result<Guid>.Failure(new Error("Designation.DuplicateTitle", $"A designation with title '{model.Title.Trim()}' already exists in this department."));
        }

        var designation = Designation.Create(model.Title.Trim(), model.DepartmentId);
        await _repository.AddAsync(designation, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(designation.DesignationId);
    }

    public async Task<Result> UpdateDesignationAsync(UpdateDesignationModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var designation = await _repository.GetByIdAsync(model.Id, cancellationToken);
        if (designation == null)
        {
            return Result.Failure(new Error("Designation.NotFound", "Designation not found."));
        }

        var department = await _deptRepository.GetByIdAsync(model.DepartmentId, cancellationToken);
        if (department == null)
        {
            return Result.Failure(new Error("Department.NotFound", "Selected department not found."));
        }

        if (department.Status != EntityStatus.Active && designation.DepartmentId != model.DepartmentId)
        {
            return Result.Failure(new Error("Department.Inactive", "Cannot move designation to an inactive department."));
        }

        if (await _repository.ExistsByTitleAsync(model.DepartmentId, model.Title.Trim(), model.Id, cancellationToken))
        {
            return Result.Failure(new Error("Designation.DuplicateTitle", $"A designation with title '{model.Title.Trim()}' already exists in this department."));
        }

        designation.Update(model.Title.Trim(), model.DepartmentId);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetDesignationStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default)
    {
        var designation = await _repository.GetByIdAsync(id, cancellationToken);
        if (designation == null)
        {
            return Result.Failure(new Error("Designation.NotFound", "Designation not found."));
        }

        if (status == EntityStatus.Inactive)
        {
            var hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.DesignationId == id && (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnNotice), cancellationToken);

            if (hasActiveEmployees)
            {
                return Result.Failure(new Error("Designation.HasActiveEmployees", "Cannot deactivate designation while active employees hold it."));
            }

            designation.Deactivate();
        }
        else
        {
            designation.Activate();
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
