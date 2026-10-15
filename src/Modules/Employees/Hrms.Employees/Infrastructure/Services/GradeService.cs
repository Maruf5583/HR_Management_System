using FluentValidation;
using Hrms.Employees.Application.Abstractions;
using Hrms.Employees.Application.Models;
using Hrms.Employees.Application.Services;
using Hrms.Employees.Domain.Entities;
using Hrms.Employees.Domain.Enums;
using Hrms.Shared.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Hrms.Employees.Infrastructure.Services;

public class GradeService : IGradeService
{
    private readonly IGradeRepository _repository;
    private readonly IDesignationRepository _designationRepository;
    private readonly IEmployeesDbContext _context;
    private readonly IValidator<CreateGradeModel> _createValidator;
    private readonly IValidator<UpdateGradeModel> _updateValidator;

    public GradeService(
        IGradeRepository repository,
        IDesignationRepository designationRepository,
        IEmployeesDbContext context,
        IValidator<CreateGradeModel> createValidator,
        IValidator<UpdateGradeModel> updateValidator)
    {
        _repository = repository;
        _designationRepository = designationRepository;
        _context = context;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<GradeDto>> GetGradesAsync(Guid? designationId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Grades
            .AsNoTracking()
            .Include(g => g.Designation)
                .ThenInclude(d => d!.Department)
            .AsQueryable();

        if (designationId.HasValue && designationId.Value != Guid.Empty)
        {
            query = query.Where(g => g.DesignationId == designationId.Value);
        }

        return await query
            .OrderBy(g => g.Designation != null && g.Designation.Department != null ? g.Designation.Department.Name : string.Empty)
            .ThenBy(g => g.Designation != null ? g.Designation.Title : string.Empty)
            .ThenBy(g => g.Level)
            .Select(g => new GradeDto(
                g.GradeId,
                g.DesignationId,
                g.Designation != null ? g.Designation.Title : string.Empty,
                g.Designation != null && g.Designation.Department != null ? g.Designation.Department.Name : string.Empty,
                g.Level,
                g.Code,
                g.Salary,
                g.Status,
                _context.Employees.Count(e => e.GradeId == g.GradeId),
                g.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<GradeDto?> GetGradeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Grades
            .AsNoTracking()
            .Include(g => g.Designation)
                .ThenInclude(d => d!.Department)
            .Where(g => g.GradeId == id)
            .Select(g => new GradeDto(
                g.GradeId,
                g.DesignationId,
                g.Designation != null ? g.Designation.Title : string.Empty,
                g.Designation != null && g.Designation.Department != null ? g.Designation.Department.Name : string.Empty,
                g.Level,
                g.Code,
                g.Salary,
                g.Status,
                _context.Employees.Count(e => e.GradeId == g.GradeId),
                g.CreatedAtUtc
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Result<Guid>> CreateGradeAsync(CreateGradeModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Guid>.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var designation = await _designationRepository.GetByIdAsync(model.DesignationId, cancellationToken);
        if (designation == null)
        {
            return Result<Guid>.Failure(new Error("Designation.NotFound", "Selected designation not found."));
        }

        if (designation.Status != EntityStatus.Active)
        {
            return Result<Guid>.Failure(new Error("Designation.Inactive", "Cannot create a grade under an inactive designation."));
        }

        if (await _repository.ExistsByCodeAsync(model.Code.Trim(), null, cancellationToken))
        {
            return Result<Guid>.Failure(new Error("Grade.DuplicateCode", $"A grade with code '{model.Code.Trim()}' already exists."));
        }

        if (await _repository.ExistsByLevelAsync(model.DesignationId, model.Level.Trim(), null, cancellationToken))
        {
            return Result<Guid>.Failure(new Error("Grade.DuplicateLevel", $"A grade with level '{model.Level.Trim()}' already exists for this designation."));
        }

        var grade = Grade.Create(model.DesignationId, model.Level.Trim(), model.Code.Trim(), model.Salary);
        await _repository.AddAsync(grade, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(grade.GradeId);
    }

    public async Task<Result> UpdateGradeAsync(UpdateGradeModel model, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(model, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(new Error("Validation.Error", validation.Errors[0].ErrorMessage));
        }

        var grade = await _repository.GetByIdAsync(model.Id, cancellationToken);
        if (grade == null)
        {
            return Result.Failure(new Error("Grade.NotFound", "Grade not found."));
        }

        var designation = await _designationRepository.GetByIdAsync(model.DesignationId, cancellationToken);
        if (designation == null)
        {
            return Result.Failure(new Error("Designation.NotFound", "Selected designation not found."));
        }

        if (designation.Status != EntityStatus.Active && grade.DesignationId != model.DesignationId)
        {
            return Result.Failure(new Error("Designation.Inactive", "Cannot move grade to an inactive designation."));
        }

        if (await _repository.ExistsByCodeAsync(model.Code.Trim(), model.Id, cancellationToken))
        {
            return Result.Failure(new Error("Grade.DuplicateCode", $"A grade with code '{model.Code.Trim()}' already exists."));
        }

        if (await _repository.ExistsByLevelAsync(model.DesignationId, model.Level.Trim(), model.Id, cancellationToken))
        {
            return Result.Failure(new Error("Grade.DuplicateLevel", $"A grade with level '{model.Level.Trim()}' already exists for this designation."));
        }

        grade.Update(model.DesignationId, model.Level.Trim(), model.Code.Trim(), model.Salary);
        await _repository.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> SetGradeStatusAsync(Guid id, EntityStatus status, CancellationToken cancellationToken = default)
    {
        var grade = await _repository.GetByIdAsync(id, cancellationToken);
        if (grade == null)
        {
            return Result.Failure(new Error("Grade.NotFound", "Grade not found."));
        }

        if (status == EntityStatus.Inactive)
        {
            var hasActiveEmployees = await _context.Employees
                .AnyAsync(e => e.GradeId == id && (e.Status == EmployeeStatus.Active || e.Status == EmployeeStatus.OnNotice), cancellationToken);

            if (hasActiveEmployees)
            {
                return Result.Failure(new Error("Grade.HasActiveEmployees", "Cannot deactivate grade while active employees hold it."));
            }

            grade.Deactivate();
        }
        else
        {
            grade.Activate();
        }

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
